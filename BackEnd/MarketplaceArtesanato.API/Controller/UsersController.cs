using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MarketplaceArtesanato.Core.Models.Requests;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly ArtesianDbContext _context;
        private readonly IStorageService _storage;
        private readonly IUserService _userService; 

        public UsersController(ArtesianDbContext context, IStorageService storage, IUserService userService)
        {
            _context = context;
            _storage = storage;
            _userService = userService;
        }

        [HttpPost("upload-photo")]
        public async Task<IActionResult> UploadPhoto(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Nenhuma imagem enviada.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId == null || role == null) return Unauthorized("Token inválido.");

            try
            {
                var imageUrl = await _storage.UploadFileAsync(file);

                await _userService.UpdateProfileImageAsync(Guid.Parse(userId), role, imageUrl);

                return Ok(new { imageUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO UPLOAD FOTO] {ex.Message}");
                return StatusCode(500, new { message = "Erro interno ao salvar a foto." });
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userId == null || role == null) return Unauthorized("Token inválido.");

            try
            {
                var success = await _userService.UpdateProfileAsync(Guid.Parse(userId), role, dto);

                if (!success) return NotFound("Usuário não encontrado.");

                return NoContent(); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO UPDATE PERFIL] {ex.Message}");
                return StatusCode(500, new { message = "Erro interno ao atualizar perfil." });
            }
        }
    }
}