// API/Controllers/ProductsController.cs
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
        public async Task<ActionResult> GetProducts( [FromQuery] string? search = null,[FromQuery] int? category = null, [FromQuery] decimal? minPrice = null, [FromQuery] decimal? maxPrice = null, [FromQuery] int page = 1,[FromQuery] int pageSize = 10)
        {
            {
                var query = _context.Products
                    .Include(p => p.Seller!)
                        .ThenInclude(s => s.Address)
                    .Include(p => p.Ratings!)
                        .ThenInclude(r => r.Customer)
                    .AsQueryable();

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

                var total = await query.CountAsync();
                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
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
        [Authorize(Roles = "Seller")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromForm] CreateProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var sellerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(sellerIdClaim, out var sellerId))
                return Unauthorized("Invalid token.");

            var seller = await _context.Sellers.FindAsync(sellerId);
            if (seller == null) return Unauthorized("Seller not found.");

            if (dto.Images == null || !dto.Images.Any())
                return BadRequest("At least one image is required.");

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
                    return BadRequest("Invalid image file.");
                }
            }

            var product = _mapper.Map<Product>(dto);
            product.Id = Guid.NewGuid();
            product.SellerId = sellerId;
            product.Seller = seller;
            product.Images = imageUrls;
            product.CreatedAt = DateTime.UtcNow;

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<ProductResponseDto>(product);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, response);
        }

        // PUT: api/products/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch.");

            var product = await _context.Products
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var sellerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (product.SellerId != sellerId) return Forbid("You can only update your own products.");

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
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Seller)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var sellerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (product.SellerId != sellerId) return Forbid("You can only delete your own products.");

            foreach (var url in product.Images)
            {
                var fileName = Path.GetFileName(new Uri(url).LocalPath);
                await _storage.DeleteAsync(fileName);
            }

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