using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Models.Requests; // Namespace do DTO
using MarketplaceArtesanato.Data.Data;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace MarketplaceArtesanato.API.Controllers
{

    [Authorize]
    [Route("api/checkout")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private readonly ArtesianDbContext _context;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IConfiguration _config;

        public CheckoutController(
            ArtesianDbContext context,
            IPublishEndpoint publishEndpoint,
            IConfiguration config)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _config = config;
        }

        [HttpPost("create-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CheckoutRequestDto request)
        {
            var items = request.Items;

            if (items == null || !items.Any())
                return BadRequest(new { message = "Carrinho vazio ou inválido." });

            var customerId = User.GetUserId();
            var domain = _config["AppUrl"] ?? "http://localhost:4200";

            var lineItems = new List<SessionLineItemOptions>();
            decimal totalAmount = 0;

            foreach (var itemDto in items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);

                if (product == null)
                    return BadRequest(new { message = $"Produto não encontrado (ID: {itemDto.ProductId})" });

                if (product.StockQuantity < itemDto.Quantity)
                    return BadRequest(new { message = $"Estoque insuficiente para: {product.Name}" });

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "brl",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = product.Name,
                        },
                        UnitAmount = (long)(product.Price * 100)
                    },
                    Quantity = itemDto.Quantity
                });

                totalAmount += product.Price * itemDto.Quantity;
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = $"{domain}/#/orders",
                CancelUrl = $"{domain}/#/cart",
                Metadata = new Dictionary<string, string>
                {
                    { "CustomerId", customerId.ToString() }
                }
            };

            try
            {
                var service = new SessionService();
                var session = await service.CreateAsync(options);

                var @event = new CheckoutInitiatedEvent
                {
                    CustomerId = customerId,
                    StripeSessionId = session.Id,
                    Total = totalAmount,
                    InitiatedAt = DateTime.UtcNow
                };

                await _publishEndpoint.Publish(@event);

                return Ok(new { sessionId = session.Id, url = session.Url });
            }
            catch (StripeException e)
            {
                return BadRequest(new { message = e.Message });
            }
        }
    }
}