using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/favorites")]
    [ApiController]
    [Authorize]
    public class FavoritesController : ControllerBase
    {
        private readonly IFavoritesService _favoritesService;

        public FavoritesController(IFavoritesService favoritesService)
        {
            _favoritesService = favoritesService;
        }

        [HttpGet]
        public async Task<ActionResult<List<Guid>>> GetFavorites()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

            try
            {
                var favoriteIds = await _favoritesService.GetFavoriteProductIdsAsync(userId);
                return Ok(favoriteIds);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO FAVORITOS GET] {ex.Message}");
                // Se a tabela não existir, o erro vai aparecer aqui
                return StatusCode(500, new { message = "Erro ao buscar favoritos." });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToFavorites([FromBody] FavoriteRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

            try
            {
                await _favoritesService.AddToFavoritesAsync(userId, dto.ProductId);
                return Ok(new { message = "Produto adicionado aos favoritos." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO FAVORITOS ADD] {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"[INNER] {ex.InnerException.Message}");

                return StatusCode(500, new { message = "Erro ao salvar favorito. Verifique se a tabela UserFavorites existe." });
            }
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromFavorites(Guid productId)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out var userId)) return Unauthorized();

            try
            {
                await _favoritesService.RemoveFromFavoritesAsync(userId, productId);
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO FAVORITOS REMOVE] {ex.Message}");
                return StatusCode(500, new { message = "Erro ao remover favorito." });
            }
        }
    }
}