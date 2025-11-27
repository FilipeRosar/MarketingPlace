using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/favorites")]
    [ApiController]
    [Authorize] // Apenas usuários logados podem favoritar
    public class FavoritesController : ControllerBase
    {
        // TODO: Injetar IFavoritesService

        [HttpGet]
        public async Task<ActionResult<List<Guid>>> GetFavorites()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            return Ok(new List<Guid>()); 
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToFavorites([FromBody] FavoriteRequestDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            return Ok(new { message = "Produto adicionado aos favoritos." });
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromFavorites(Guid productId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();


            return NoContent(); 
        }
    }
}