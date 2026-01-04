using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register/customer")]
        public async Task<ActionResult> RegisterCustomer([FromBody] RegisterCostumerDto dto)
        {
            var result = await _authService.RegisterCustomerAsync(dto);
            if (!result.Success)
            {
                return Conflict(new { message = result.Message });
            }
            return Ok(result);
        }

        [HttpPost("register/seller")]
        public async Task<ActionResult> RegisterSeller([FromBody] RegisterSellerDto dto)
        {
            var result = await _authService.RegisterSellerAsync(dto);
            if (!result.Success)
            {
                return Conflict(new { message = result.Message });
            }
            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var result = await _authService.LoginAsync(dto);
            if (!result.Success)
            {
                return Unauthorized(new { message = result.Message });
            }
            return Ok(result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var result = await _authService.ForgotPasswordAsync(dto);
            if (!result.Success)
            {

                return NotFound(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }
    }
}