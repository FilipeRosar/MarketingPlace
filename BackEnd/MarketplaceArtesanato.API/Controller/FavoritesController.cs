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

            // Simulação: Chamada ao serviço para buscar lista de IDs de produtos favoritos
            // var favoriteIds = await _favoritesService.GetFavoriteProductIdsAsync(Guid.Parse(userId));

            // Retorna a lista de IDs de produtos que o usuário favoritou
            return Ok(new List<Guid>()); // Retorna lista vazia por enquanto
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToFavorites([FromBody] FavoriteRequestDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            // Lógica de serviço: Verifica se já existe, salva no banco.
            // await _favoritesService.AddToFavoritesAsync(Guid.Parse(userId), dto.ProductId);

            return Ok(new { message = "Produto adicionado aos favoritos." });
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromFavorites(Guid productId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            // Lógica de serviço: Remove a entrada no banco
            // await _favoritesService.RemoveFromFavoritesAsync(Guid.Parse(userId), productId);

            return NoContent(); // 204 Sucesso sem conteúdo
        }
    }
}