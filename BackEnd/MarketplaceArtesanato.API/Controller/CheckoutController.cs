using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace MarketplaceArtesanato.API.Controllers
{
    [Authorize(Roles = "Customer")]
    [Route("api/checkout")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IConfiguration _config;

        public CheckoutController(
            ICartService cartService,
            IPublishEndpoint publishEndpoint,
            IConfiguration config)
        {
            _cartService = cartService;
            _publishEndpoint = publishEndpoint;
            _config = config;
        }

        [HttpPost("create-session")]
        public async Task<IActionResult> CreateCheckoutSession()
        {
            var customerId = User.GetUserId();
            var cart = await _cartService.GetCartAsync(customerId);

            if (cart == null || !cart.Items.Any())
                return BadRequest(new { message = "Carrinho vazio" });

            var domain = _config["AppUrl"] ?? "http://localhost:4200";

            var lineItems = new List<SessionLineItemOptions>();

            foreach (var i in cart.Items)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "brl",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = i.ProductName,
                            Images = !string.IsNullOrEmpty(i.ProductImage) ? new List<string> { i.ProductImage } : null 
                        },
                        UnitAmount = (long)(i.Price * 100) // centavos
                    },
                    Quantity = i.Quantity
                });
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = $"{domain}/#/?checkout=success&session_id={{CHECKOUT_SESSION_ID}}", 
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
                    Items = cart.Items.Select(i => new CheckoutItemEvent
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.Price
                    }).ToList(),
                    Total = cart.TotalPrice,
                    InitiatedAt = DateTime.UtcNow
                };

                await _publishEndpoint.Publish(@event);

                return Ok(new
                {
                    sessionId = session.Id,
                    url = session.Url,
                    message = "Sessão criada. Redirecione o cliente para o pagamento."
                });
            }
            catch (StripeException e)
            {
                return BadRequest(new { message = e.Message });
            }
        }
    }
}