using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceArtesanato.API.Controller
{
    [Route("api/ratings")]
    [ApiController]
    public class RatingsController : ControllerBase
    {
        private readonly ArtesianDbContext _context;

        public RatingsController(ArtesianDbContext context)
        {
            _context = context;
        }

        [HttpGet("product/{productId:guid}")]
        public async Task<IActionResult> GetByProduct(Guid productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var baseQuery = _context.Ratings
                .Include(r => r.Customer)
                .ThenInclude(c => c.User)
                .Where(r => !r.IsDeleted && r.ProductId == productId);

            var total = await baseQuery.CountAsync();
            var avg = total > 0 ? await baseQuery.AverageAsync(r => (double)r.Stars) : 0.0;

            var ratings = await baseQuery
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new
                {
                    id = r.Id,
                    customerId = r.CustomerId,
                    customerName = r.Customer.User.Name ?? "Cliente",
                    stars = r.Stars,
                    review = r.Review,
                    sellerReply = r.SellerReply,
                    sellerReplyAt = r.SellerReplyAt,
                    createdAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                data = ratings,
                total,
                averageRating = Math.Round(avg, 1),
                page,
                pageSize,
                pages = (int)Math.Ceiling(total / (double)pageSize)
            });
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        public async Task<IActionResult> CreateRating([FromBody] CreateRatingDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.GetUserId();
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return NotFound(new { message = "Cliente nao encontrado." });

            var productExists = await _context.Products.AnyAsync(p => p.Id == dto.ProductId && !p.IsDeleted);
            if (!productExists) return NotFound(new { message = "Produto nao encontrado." });

            var existing = await _context.Ratings
                .FirstOrDefaultAsync(r => r.CustomerId == customer.Id && r.ProductId == dto.ProductId);
            if (existing != null)
                return Conflict(new { message = "Voce ja avaliou este produto." });

            var rating = new Rating
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                ProductId = dto.ProductId,
                Stars = dto.Stars,
                Review = dto.Review?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            return StatusCode(201, new { message = "Avaliacao enviada com sucesso.", ratingId = rating.Id });
        }

        [Authorize(Roles = "Seller")]
        [HttpPost("{id:guid}/reply")]
        public async Task<IActionResult> ReplyToRating(Guid id, [FromBody] AddRatingReplyDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.GetUserId();
            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null) return NotFound(new { message = "Vendedor nao encontrado." });

            var rating = await _context.Ratings
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (rating == null) return NotFound(new { message = "Avaliacao nao encontrada." });

            if (rating.Product.SellerId != seller.Id)
                return Forbid();

            var reply = dto.Reply?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(reply))
                return BadRequest(new { message = "Resposta nao pode ser vazia." });

            rating.SellerReply = reply;
            rating.SellerReplyAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Resposta enviada com sucesso." });
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateRating(Guid id, [FromBody] UpdateRatingDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var isAdmin = User.IsInRole("Admin");
            Rating? rating;

            if (isAdmin)
            {
                rating = await _context.Ratings.FirstOrDefaultAsync(r => r.Id == id);
            }
            else
            {
                var userId = User.GetUserId();
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null) return NotFound(new { message = "Cliente nao encontrado." });

                rating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customer.Id);
            }

            if (rating == null) return NotFound(new { message = "Avaliacao nao encontrada." });

            rating.Stars = dto.Stars;
            rating.Review = dto.Review?.Trim() ?? string.Empty;
            rating.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Avaliacao atualizada com sucesso." });
        }

        [Authorize(Roles = "Customer,Admin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteRating(Guid id)
        {
            var isAdmin = User.IsInRole("Admin");
            Rating? rating;

            if (isAdmin)
            {
                rating = await _context.Ratings.FirstOrDefaultAsync(r => r.Id == id);
            }
            else
            {
                var userId = User.GetUserId();
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null) return NotFound(new { message = "Cliente nao encontrado." });

                rating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == customer.Id);
            }

            if (rating == null) return NotFound(new { message = "Avaliacao nao encontrada." });

            _context.Ratings.Remove(rating);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Avaliacao removida com sucesso." });
        }
    }
}
