using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceArtesanato.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PromotionsController : ControllerBase
    {
        private readonly ArtesianDbContext _context;
        private readonly ILogger<PromotionsController> _logger;

        public PromotionsController(
            ArtesianDbContext context,
            ILogger<PromotionsController> logger)
        {
            _context = context;
            _logger = logger;
        }


        [HttpGet("my-promotions")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<List<PromotionDto>>> GetMyPromotions()
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == userId.Value);

            if (seller == null)
                return NotFound("Seller not found");

            var promotions = await _context.Promotions
                .Where(p => p.SellerId == seller.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var dtos = promotions.Select(p => new PromotionDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                DiscountPercentage = p.DiscountPercentage,
                ProductIds = p.ProductIds,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();

            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PromotionDto>> GetPromotion(Guid id)
        {
            var promotion = await _context.Promotions.FindAsync(id);

            if (promotion == null)
                return NotFound();

            if (User.IsInRole("Seller"))
            {
                var userId = GetUserId();
                var seller = await _context.Sellers
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (seller == null || promotion.SellerId != seller.Id)
                    return Forbid();
            }

            return Ok(ToDto(promotion));
        }

        [HttpPost]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<PromotionDto>> CreatePromotion([FromBody] CreatePromotionDto dto)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == userId.Value);

            if (seller == null)
                return NotFound("Seller not found");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Nome é obrigatório");

            if (dto.DiscountPercentage <= 0 || dto.DiscountPercentage > 90)
                return BadRequest("Desconto deve estar entre 1% e 90%");

            if (dto.StartDate >= dto.EndDate)
                return BadRequest("Data de início deve ser anterior à data de fim");

            if (dto.ProductIds == null || dto.ProductIds.Count == 0)
                return BadRequest("Selecione pelo menos um produto");

            var productCount = await _context.Products
                .CountAsync(p => dto.ProductIds.Contains(p.Id) && p.SellerId == seller.Id);

            if (productCount != dto.ProductIds.Count)
                return BadRequest("Um ou mais produtos não pertencem a você");

            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                SellerId = seller.Id,
                Name = dto.Name,
                Description = dto.Description,
                DiscountPercentage = dto.DiscountPercentage,
                ProductIds = dto.ProductIds,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = dto.IsActive
            };

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Promoção {PromotionId} criada pelo seller {SellerId}",
                promotion.Id, seller.Id);

            return CreatedAtAction(nameof(GetPromotion), new { id = promotion.Id }, ToDto(promotion));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<PromotionDto>> UpdatePromotion(Guid id, [FromBody] UpdatePromotionDto dto)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == userId.Value);

            if (seller == null)
                return NotFound("Seller not found");

            var promotion = await _context.Promotions.FindAsync(id);

            if (promotion == null)
                return NotFound();

            if (promotion.SellerId != seller.Id)
                return Forbid();

            // Atualiza campos
            if (dto.Name != null)
                promotion.Name = dto.Name;

            if (dto.Description != null)
                promotion.Description = dto.Description;

            if (dto.DiscountPercentage.HasValue)
            {
                if (dto.DiscountPercentage.Value <= 0 || dto.DiscountPercentage.Value > 90)
                    return BadRequest("Desconto deve estar entre 1% e 90%");

                promotion.DiscountPercentage = dto.DiscountPercentage.Value;
            }

            if (dto.ProductIds != null && dto.ProductIds.Count > 0)
            {
                // Verifica se os produtos pertencem ao seller
                var productCount = await _context.Products
                    .CountAsync(p => dto.ProductIds.Contains(p.Id) && p.SellerId == seller.Id);

                if (productCount != dto.ProductIds.Count)
                    return BadRequest("Um ou mais produtos não pertencem a você");

                promotion.ProductIds = dto.ProductIds;
            }

            if (dto.StartDate.HasValue)
                promotion.StartDate = dto.StartDate.Value;

            if (dto.EndDate.HasValue)
                promotion.EndDate = dto.EndDate.Value;

            if (dto.IsActive.HasValue)
                promotion.IsActive = dto.IsActive.Value;

            // Valida datas
            if (promotion.StartDate >= promotion.EndDate)
                return BadRequest("Data de início deve ser anterior à data de fim");

            promotion.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Promoção {PromotionId} atualizada pelo seller {SellerId}",
                promotion.Id, seller.Id);

            return Ok(ToDto(promotion));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult> DeletePromotion(Guid id)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized();

            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == userId.Value);

            if (seller == null)
                return NotFound("Seller not found");

            var promotion = await _context.Promotions.FindAsync(id);

            if (promotion == null)
                return NotFound();

            if (promotion.SellerId != seller.Id)
                return Forbid();

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Promoção {PromotionId} deletada pelo seller {SellerId}",
                id, seller.Id);

            return NoContent();
        }

        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<PromotionDto>>> GetActivePromotionsForProduct(Guid productId)
        {
            var now = DateTime.UtcNow;

            var promotions = await _context.Promotions
                .Where(p => p.IsActive &&
                           p.ProductIds.Contains(productId) &&
                           p.StartDate <= now &&
                           p.EndDate >= now)
                .OrderByDescending(p => p.DiscountPercentage)
                .ToListAsync();

            var dtos = promotions.Select(ToDto).ToList();

            return Ok(dtos);
        }

        // Helper methods
        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;
            return null;
        }

        private PromotionDto ToDto(Promotion promotion)
        {
            return new PromotionDto
            {
                Id = promotion.Id,
                Name = promotion.Name,
                Description = promotion.Description,
                DiscountPercentage = promotion.DiscountPercentage,
                ProductIds = promotion.ProductIds,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                IsActive = promotion.IsActive,
                CreatedAt = promotion.CreatedAt,
                UpdatedAt = promotion.UpdatedAt
            };
        }
    }

    // DTOs
    public class PromotionDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DiscountPercentage { get; set; }
        public List<Guid> ProductIds { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreatePromotionDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal DiscountPercentage { get; set; }
        public List<Guid> ProductIds { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdatePromotionDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public List<Guid>? ProductIds { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
    }
}