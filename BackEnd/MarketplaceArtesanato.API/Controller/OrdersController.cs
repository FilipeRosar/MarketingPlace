using MarketplaceArtesanato.API.Extensions; 
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpGet("my-orders")]
        [Authorize]
        public async Task<ActionResult<List<OrderResponseDto>>> GetMyOrders()
        {
            var userId = User.GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";

            var orders = await _orderService.GetByUserAsync(userId, role);
            return Ok(orders);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<OrderResponseDto>> GetOrder(Guid id)
        {
            try
            {
                var userId = User.GetUserId();
                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";

                var order = await _orderService.GetByIdAsync(id, userId, role);
                return Ok(order);
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("checkout")]
        [Authorize]
        public async Task<ActionResult> Checkout([FromBody] CheckoutRequestDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var result = await _orderService.CreateOrderAsync(userId, dto);

                return Ok(result);
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO CHECKOUT] {ex.Message}");
                return StatusCode(500, "Erro ao processar pedido.");
            }
        }

        [HttpPut("{id}/tracking")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UpdateTracking(Guid id, [FromBody] UpdateTrackingDto dto)
        {
            try
            {
                var userId = User.GetUserId();
                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

                await _orderService.UpdateTrackingAsync(id, userId, role, dto.TrackingCode);

                return Ok(new { message = "Rastreamento atualizado com sucesso." });
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
        }
    }
}