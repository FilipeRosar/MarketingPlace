using AutoMapper;
using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ArtesianDbContext _context;
        private readonly IMapper _mapper;
        private readonly IStorageService _storage;

        public ProductsController(ArtesianDbContext context, IMapper mapper, IStorageService storage)
        {
            _context = context;
            _mapper = mapper;
            _storage = storage;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult> GetProducts(
            [FromQuery] string? search = null,
            [FromQuery] int? category = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] Guid? sellerId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Products
                .Include(p => p.Seller!)
                    .ThenInclude(s => s.Address)
                .Include(p => p.Ratings!)
                    .ThenInclude(r => r.Customer)
                .AsQueryable();

            query = query.Where(p => !p.IsDeleted);

            if (sellerId.HasValue)
            {
                query = query.Where(p => p.SellerId == sellerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(lowerSearch) ||
                    (p.Description != null && p.Description.ToLower().Contains(lowerSearch)) ||
                    p.Seller!.Name.ToLower().Contains(lowerSearch));
            }

            if (category.HasValue)
                query = query.Where(p => (int)p.Category == category.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            var total = await query.CountAsync(cancellationToken);

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var dtos = _mapper.Map<List<ProductResponseDto>>(products);

            return Ok(new
            {
                data = dtos,
                total,
                page,
                pageSize,
                pages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Seller!)
                    .ThenInclude(s => s.Address)
                .Include(p => p.Ratings!)
                    .ThenInclude(r => r.Customer)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var dto = _mapper.Map<ProductResponseDto>(product);
            return Ok(dto);
        }

        // POST: api/products
        [HttpPost]
        [Authorize(Roles = "Seller,Admin")] 
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromForm] CreateProductDto dto)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                if (!Guid.TryParse(userIdString, out var userId))
                    return Unauthorized("Token inválido ou ID do usuário não encontrado.");


                var seller = await _context.Sellers.FindAsync(userId);
                if (seller == null) return Unauthorized("Vendedor não encontrado. Se você é Admin, precisa ter um perfil de Vendedor também.");

                if (dto.Images == null || !dto.Images.Any())
                    return BadRequest("Pelo menos uma imagem é obrigatória.");

                var imageUrls = new List<string>();

                foreach (var file in dto.Images)
                {
                    if (file.Length > 0 && IsImage(file))
                    {
                        var url = await _storage.UploadFileAsync(file);
                        imageUrls.Add(url);
                    }
                    else
                    {
                        return BadRequest($"Arquivo inválido: {file.FileName}. Apenas imagens são permitidas.");
                    }
                }

                var product = _mapper.Map<Product>(dto);
                product.Id = Guid.NewGuid();
                product.SellerId = userId;
                product.Seller = seller;
                product.Images = imageUrls;
                product.CreatedAt = DateTime.UtcNow;
                product.IsDeleted = false;

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                var response = _mapper.Map<ProductResponseDto>(product);
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO CRÍTICO] Falha ao criar produto: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"[INNER] {ex.InnerException.Message}");

                return StatusCode(500, new { message = "Erro interno ao processar o produto.", details = ex.Message });
            }
        }

        // PUT: api/products/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch.");

            var product = await _context.Products
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (product.SellerId != userId && role != "Admin")
                return Forbid("Você não tem permissão para editar este produto.");

            _mapper.Map(dto, product);
            product.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "Seller,Admin")] 
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var role = User.FindFirst(ClaimTypes.Role)?.Value;


            if (product.SellerId != userId && role != "Admin")
                return Forbid("Você não tem permissão para excluir este produto.");


            foreach (var url in product.Images)
            {
                try
                {
                    await _storage.DeleteAsync(url);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AVISO] Falha ao deletar imagem do Azure: {ex.Message}");
                }
            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool IsImage(IFormFile file)
        {
            try
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                return new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp" }.Contains(ext);
            }
            catch
            {
                return false;
            }
        }

        private bool ProductExists(Guid id) => _context.Products.Any(e => e.Id == id);
    }
}