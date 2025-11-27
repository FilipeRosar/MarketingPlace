using MarketplaceArtesanato.Core.Entities.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceArtesanato.API.Controller
{
    [Route("api/ratings")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class RatingsController : ControllerBase
    {
        // TODO: Injetar IRatingService e IProductService

        // [POST] /api/ratings
        [HttpPost]
        public async Task<IActionResult> CreateRating([FromBody] CreateRatingDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            // 2. Lógica de Serviço: Verificar se o cliente já comprou o produto e salvar o rating.
            // if (!await _ratingService.CanUserRateProduct(userId, dto.ProductId)) return Forbid("Somente compradores podem avaliar.");

            // var rating = await _ratingService.CreateRatingAsync(userId, dto);

            return StatusCode(201, new { message = "Avaliação enviada com sucesso (Simulação)." });
        }
    }
}
