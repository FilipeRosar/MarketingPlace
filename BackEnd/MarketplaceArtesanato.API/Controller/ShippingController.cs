using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceArtesanato.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingController : ControllerBase
    {
        private readonly IShippingService _shippingService;

        public ShippingController(IShippingService shippingService)
        {
            _shippingService = shippingService;
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromBody] CalculateShippingRequest request)
        {
            var options = await _shippingService.CalculateShippingAsync(request);
            return Ok(options);
        }

        [Authorize(Roles = "Seller")] 
        [HttpPost("generate-label")]
        public async Task<IActionResult> GenerateLabel([FromBody] GenerateLabelRequest request)
        {
            var url = await _shippingService.GenerateLabelAsync(request);
            return Ok(new { labelUrl = url });
        }
    }
}
