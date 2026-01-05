using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/sellers")]
    [ApiController]
    public class SellersController : ControllerBase
    {
        private readonly ISellerService _sellerService;
        private readonly IStorageService _storageService;

        public SellersController(ISellerService sellerService, IStorageService storageService)
        {
            _sellerService = sellerService;
            _storageService = storageService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SellerResponseDto>> GetSeller(Guid id)
        {
            var seller = await _sellerService.GetByIdAsync(id);

            if (seller == null)
                return NotFound("Vendedor não encontrado no banco de dados.");

            return Ok(seller);
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<SellerResponseDto>> GetSellerByUserId(Guid userId)
        {
            var seller = await _sellerService.GetByUserIdAsync(userId);

            if (seller == null)
                return NotFound("Vendedor não encontrado para este usuário.");

            return Ok(seller);
        }

        [HttpPost("{sellerId}/moments/upload-video")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<object>> UploadMomentVideo(Guid sellerId, IFormFile video)
        {
            if (video == null || video.Length == 0)
                return BadRequest("Vídeo é obrigatório.");

            try
            {
                var videoUrl = await _storageService.UploadVideoAsync(video);
                return Ok(new { videoUrl });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Erro ao fazer upload do vídeo.");
            }
        }

        // POST: api/sellers/{sellerId}/moments/upload-thumb
        [HttpPost("{sellerId}/moments/upload-thumb")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<object>> UploadMomentThumb(Guid sellerId, IFormFile thumb)
        {
            if (thumb == null || thumb.Length == 0)
                return Ok(new { imageUrl = "" }); // thumbnail é opcional

            try
            {
                var imageUrl = await _storageService.UploadFileAsync(thumb); // reutiliza o método de imagem
                return Ok(new { imageUrl });
            }
            catch (Exception)
            {
                return StatusCode(500, "Erro ao fazer upload da thumbnail.");
            }
        }

        // POST: api/sellers/{sellerId}/moments → Cria o momento
        [HttpPost("{sellerId}/moments")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<MomentResponseDto>> CreateMoment(Guid sellerId, [FromBody] CreateMomentDto dto)
        {
            var currentUserId = User.GetUserId(); 

            var seller = await _sellerService.GetByIdAsync(sellerId);
            var isOwner = await _sellerService.IsOwnerAsync(sellerId, currentUserId);
            if (!isOwner)
                return Forbid("Você não tem permissão para publicar momentos nesta loja.");

            var createdMoment = await _sellerService.CreateMomentAsync(sellerId, dto);

            return CreatedAtAction(nameof(GetMoments), new { sellerId }, createdMoment);
        }

        [HttpGet("{sellerId}/moments")]
        public async Task<ActionResult<List<MomentResponseDto>>> GetMoments(Guid sellerId)
        {
            var moments = await _sellerService.GetMomentsAsync(sellerId);
            return Ok(moments);
        }

        [HttpGet("dashboard")] 
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<SellerDashboardDto>> GetDashboard()
        {
            
                // Tente capturar o ID assim para garantir que o JWT está sendo lido
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized("Token inválido ou sem ID de usuário.");

                var userId = Guid.Parse(userIdClaim);
                var dashboard = await _sellerService.GetDashboardAsync(userId);

                if (dashboard == null)
                    return NotFound("Perfil de vendedor não encontrado para este usuário.");

               return Ok(dashboard);
            
        }

    }
}