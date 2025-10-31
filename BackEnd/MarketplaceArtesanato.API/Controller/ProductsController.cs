using AutoMapper;
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
    [Route("api/[controller]")]
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
        public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetProducts(
            [FromQuery] string? search = null,
            [FromQuery] int? category = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Products
                .Include(p => p.Seller!)
                    .ThenInclude(s => s.Address)
                .Include(p => p.Ratings)
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search) || p.Description!.Contains(search));

            if (category.HasValue)
                query = query.Where(p => (int)p.Category == category.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // Paginação
            var total = await query.CountAsync();
            var products = await query
                .OrderBy(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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
                .Include(p => p.Ratings)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var dto = _mapper.Map<ProductResponseDto>(product);
            return Ok(dto);
        }

        // POST: api/products
        [HttpPost]
        [Authorize(Roles = "Seller")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromForm] CreateProductDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // TODO: Pegar SellerId do JWT
            var sellerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (sellerIdClaim == null || !Guid.TryParse(sellerIdClaim.Value, out var sellerId))
            {
                return Unauthorized("Invalid seller ID.");
            }
       

            if (!await _context.Users.AnyAsync(u => u.Id == sellerId))
            {
                return BadRequest("Seller not found");
            }
            if (dto.Images == null || !dto.Images.Any())
            {
                return BadRequest("At least one image is required.");
            }

            var imageUrls = new List<string>();
            foreach (var file in dto.Images)
            {
                if (file.Length > 0 && IsImage(file))
                {
                    var imageUrl = await _storage.UploadFileAsync(file);
                    imageUrls.Add(imageUrl);
                }
                else
                {
                    return BadRequest("Invalid image file.");
                }
            }

            var product = _mapper.Map<Product>(dto);
            product.Id = Guid.NewGuid();
            product.CreatedAt = DateTime.UtcNow;
            product.SellerId = sellerId;
            product.Images = imageUrls;


            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<ProductResponseDto>(product);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
        }

        // PUT: api/products/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch.");

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // TODO: Verificar se é o dono (SellerId == JWT)
            // if (product.SellerId != User.GetUserId()) return Forbid();

            _mapper.Map(dto, product);
            product.CreatedAt = DateTime.UtcNow; 

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
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            // TODO: Apenas o dono ou admin
            // if (product.SellerId != User.GetUserId()) return Forbid();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        private bool IsImage(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(ext);
        }
        private bool ProductExists(Guid id) => _context.Products.Any(e => e.Id == id);
    }
}