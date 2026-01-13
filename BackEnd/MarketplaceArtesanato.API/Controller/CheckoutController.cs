using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Core.Interfaces;
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
        private readonly IPlatformFeeService _platformFeeService;
        private readonly IPriceCalculationService _priceCalculationService;

        private const long SERVICE_FEE_CENTS = 299; 

        public CheckoutController(
            ArtesianDbContext context,
            IPublishEndpoint publishEndpoint,
            IConfiguration config,
            IPlatformFeeService platformFeeService,
            IPriceCalculationService priceCalculationService)
        {
            _context = context;
            _publishEndpoint = publishEndpoint;
            _config = config;
            _platformFeeService = platformFeeService;
            _priceCalculationService = priceCalculationService;
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
            var shippingFee = request.ShippingFee;
            var shippingName = string.IsNullOrWhiteSpace(request.ShippingName) ? "Frete" : request.ShippingName.Trim();
            // Lista para o evento
            var eventItems = new List<CheckoutItemEvent>();

            var cartItems = new List<(MarketplaceArtesanato.Core.Entities.Product Product, CheckoutItemDto Item, decimal ItemTotal)>();
            var sellerTotals = new Dictionary<Guid, decimal>();

            foreach (var itemDto in items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null) return BadRequest($"Produto {itemDto.ProductId} n?o encontrado.");
                if (product.StockQuantity < itemDto.Quantity) return BadRequest($"Estoque insuficiente: {product.Name}");

                var priceResult = await _priceCalculationService.CalculateProductPriceAsync(product, customerId);
                var unitPrice = priceResult.FinalPrice;
                decimal itemTotal = unitPrice * itemDto.Quantity;

                cartItems.Add((Product: product, Item: itemDto, ItemTotal: itemTotal));

                if (sellerTotals.ContainsKey(product.SellerId))
                    sellerTotals[product.SellerId] += itemTotal;
                else
                    sellerTotals[product.SellerId] = itemTotal;

                totalProductAmount += itemTotal;
            }

            var sellerRates = new Dictionary<Guid, decimal>();
            foreach (var (sellerId, sellerTotal) in sellerTotals)
            {
                var rate = await _platformFeeService.GetCommissionRateAsync(sellerId, sellerTotal);
                sellerRates[sellerId] = rate;
            }

            foreach (var cartItem in cartItems)
            {
                var product = cartItem.Product;
                var itemDto = cartItem.Item;
                var itemTotal = cartItem.ItemTotal;
                var commissionRate = sellerRates[product.SellerId] / 100m;
                var commission = itemTotal * commissionRate;

                if (order.SellerCommissions.ContainsKey(product.SellerId))
                    order.SellerCommissions[product.SellerId] += commission;
                else
                    order.SellerCommissions[product.SellerId] = commission;

                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "brl",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = product.Name,
                            Images = product.Images?.Any() == true ? new List<string> { product.Images[0].Url } : null
                        },
                        UnitAmount = (long)(itemTotal / itemDto.Quantity * 100)
                    },
                    Quantity = itemDto.Quantity
                });

                order.Items.Add(new Core.Entities.OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemTotal / itemDto.Quantity,
                    OrderId = order.Id
                });

                eventItems.Add(new CheckoutItemEvent
                {
                    ProductId = product.Id,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemTotal / itemDto.Quantity,
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

            if (shippingFee > 0)
            {
                lineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "brl",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = shippingName
                        },
                        UnitAmount = (long)(shippingFee * 100)
                    },
                    Quantity = 1
                });
            }

            order.TotalAmount = totalProductAmount + (SERVICE_FEE_CENTS / 100m) + shippingFee;

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
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    TransferGroup = order.Id.ToString(),
                    Metadata = new Dictionary<string, string>
                    {
                        { "Type", "order" },
                        { "OrderId", order.Id.ToString() },
                        { "CustomerId", customerId.ToString() }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    { "Type", "order" },
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
