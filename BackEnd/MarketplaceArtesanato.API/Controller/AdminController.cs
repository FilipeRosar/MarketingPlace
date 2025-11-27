using AutoMapper;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")] 
    public class AdminController : ControllerBase
    {
        private readonly ArtesianDbContext _context;

        public AdminController(ArtesianDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/pending-sellers
        [HttpGet("pending-sellers")]
        public async Task<ActionResult<IEnumerable<Seller>>> GetPendingSellers()
        {

            var pendingSellers = await _context.Sellers
                                             .Include(s => s.Address)
                                             .Where(s => !s.IsDeleted && s.Products.Count == 0)
                                             .ToListAsync();

            return Ok(pendingSellers);
        }

        // POST: api/admin/approve-seller/{id}
        [HttpPost("approve-seller/{id}")]
        public async Task<IActionResult> ApproveSeller(Guid id)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller == null || seller.IsDeleted) return NotFound();

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Vendedor {seller.Name} aprovado com sucesso." });
        }
    }
}