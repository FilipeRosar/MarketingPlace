using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AuthController(
            IAuthService authService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _authService = authService;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpPost("register/customer")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
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
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult> RegisterSeller([FromBody] RegisterSellerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterSellerAsync(dto);
            if (!result.Success)
            {
                return Conflict(new { message = result.Message });
            }
            return Ok(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!await ValidateTurnstileAsync(dto.TurnstileToken))
                return Unauthorized(new { message = "Falha na verificação de segurança." });

            var result = await _authService.LoginAsync(dto);

            if (!result.Success)
                return Unauthorized(new { message = result.Message });

            return Ok(result);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _authService.ForgotPasswordAsync(dto);
            if (!result.Success)
            {

                return NotFound(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var result = await _authService.ResetPasswordAsync(dto);
            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }
            return Ok(new { message = result.Message });
        }
        //[HttpGet("me")]
        //[Authorize]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //public async Task<IActionResult> GetCurrentUser()
        //{
        //    var userId = User.GetUserId();

        //    var user = await _authService.GetCurrentUserAsync(userId);

        //    if (user == null)
        //        return Unauthorized();

        //    return Ok(user);
        //}
        private async Task<bool> ValidateTurnstileAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            var client = _httpClientFactory.CreateClient();

            var secretKey = _configuration["Turnstile:SecretKey"];

            var response = await client.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = secretKey,
                    ["response"] = token
                })
            );

            if (!response.IsSuccessStatusCode)
                return false;

            var json = await response.Content.ReadAsStringAsync();

            return json.Contains("\"success\":true");
        }

    }
}
