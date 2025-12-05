using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Models.Requests;
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

        // 1. COMISSÃO: 15% retido do valor do produto (cobrado do vendedor)
        private const decimal PLATFORM_COMMISSION_RATE = 0.15m;

        // 2. TAXA DE SERVIÇO: Valor fixo cobrado do comprador (ex: R$ 2,99)
        private const long SERVICE_FEE_CENTS = 299; // R$ 2,99

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
            if (items == null || !items.Any()) return BadRequest(new { message = "Carrinho vazio." });

            var customerId = User.GetUserId();
            var domain = _config["AppUrl"] ?? "http://localhost:4200";

            var order = new Core.Entities.Order
            {
                Id = Guid.NewGuid(),
                BuyerId = customerId,
                Status = Core.Entities.Enums.OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                SellerCommissions = new Dictionary<Guid, decimal>()
            };

            var lineItems = new List<SessionLineItemOptions>();
            decimal totalProductAmount = 0;
            // Lista para o evento
            var eventItems = new List<CheckoutItemEvent>();

            foreach (var itemDto in items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null) return BadRequest($"Produto {itemDto.ProductId} não encontrado.");
                if (product.StockQuantity < itemDto.Quantity) return BadRequest($"Estoque insuficiente: {product.Name}");

                decimal itemTotal = product.Price * itemDto.Quantity;
                decimal commission = itemTotal * PLATFORM_COMMISSION_RATE;


                decimal sellerAmount = itemTotal - commission;

                if (order.SellerCommissions.ContainsKey(product.SellerId))
                    order.SellerCommissions[product.SellerId] += sellerAmount;
                else
                    order.SellerCommissions[product.SellerId] = sellerAmount;

                totalProductAmount += itemTotal;

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "brl",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = product.Name,
                            Images = product.Images?.Any() == true ? new List<string> { product.Images[0] } : null
                        },
                        UnitAmount = (long)(product.Price * 100)
                    },
                    Quantity = itemDto.Quantity
                });

                order.Items.Add(new Core.Entities.OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,
                    OrderId = order.Id
                });

                eventItems.Add(new CheckoutItemEvent
                {
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,
                    CommissionAmount = commission 
                });
            }

            lineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "brl",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = "Taxa de Serviço",
                        Description = "Manutenção da plataforma e segurança",
                    },
                    UnitAmount = SERVICE_FEE_CENTS
                },
                Quantity = 1
            });

            order.TotalAmount = totalProductAmount + (SERVICE_FEE_CENTS / 100m);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card", "boleto" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = $"{domain}/#/orders",
                CancelUrl = $"{domain}/#/cart",
                BillingAddressCollection = "required",
                ShippingAddressCollection = new SessionShippingAddressCollectionOptions { AllowedCountries = new List<string> { "BR" } },
                Metadata = new Dictionary<string, string>
                {
                    { "OrderId", order.Id.ToString() },
                    { "CustomerId", customerId.ToString() }
                }
            };

            try
            {
                var service = new SessionService();
                var session = await service.CreateAsync(options);

                order.StripeSessionId = session.Id;
                await _context.SaveChangesAsync();

                decimal totalPlatformCommission = eventItems.Sum(i => i.CommissionAmount) + (SERVICE_FEE_CENTS / 100m);

                var @event = new CheckoutInitiatedEvent
                {
                    CustomerId = customerId,
                    StripeSessionId = session.Id,
                    Total = order.TotalAmount,
                    PlatformFee = totalPlatformCommission, 
                    Items = eventItems,
                    InitiatedAt = DateTime.UtcNow
                };
                await _publishEndpoint.Publish(@event);

                return Ok(new { sessionId = session.Id, url = session.Url });
            }
            catch (StripeException e)
            {
                // Rollback se o Stripe falhar
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Erro Stripe: {e.StripeError.Message}");
                return BadRequest(new { message = "Erro no pagamento: " + e.StripeError.Message });
            }
        }
    }
}