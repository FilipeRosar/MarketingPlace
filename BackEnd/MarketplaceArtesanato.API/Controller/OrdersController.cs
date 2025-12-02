using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ArtesianDbContext _context;
        private readonly StripePaymentService _stripeService; // Descomente se for usar

        public OrdersController(ArtesianDbContext context /*, StripePaymentService stripeService */)
        {
            _context = context;
            // _stripeService = stripeService;
        }

        [HttpPost("checkout")]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult> Checkout([FromBody] CheckoutRequestDto dto)
        {
            // Obtém o ID do usuário logado
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out var customerId))
                return Unauthorized("Usuário inválido.");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = customerId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>()
            };

            decimal totalAmount = 0;

            foreach (var itemDto in dto.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);

                if (product == null)
                    return BadRequest(new { message = $"Produto {itemDto.ProductId} não encontrado" });

                if (product.StockQuantity < itemDto.Quantity)
                    return BadRequest(new { message = $"Estoque insuficiente para {product.Name}" });

                // Atualiza total
                totalAmount += product.Price * itemDto.Quantity;

                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,
                    OrderId = order.Id
                });
            }

            order.TotalAmount = totalAmount; 

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Retorna os dados do pedido criado
            return Ok(new { order.Id, order.TotalAmount, status = order.Status.ToString() });
        }
    }
}