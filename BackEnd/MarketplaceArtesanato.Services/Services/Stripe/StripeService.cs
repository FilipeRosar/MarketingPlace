using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace MarketplaceArtesanato.Services.Services
{
    

    public class StripePaymentService : IStripePaymentService
    {
        private readonly IConfiguration _config;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ArtesianDbContext _context; 
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(
            IConfiguration config,
            IPublishEndpoint publishEndpoint,
            ArtesianDbContext context,
            ILogger<StripePaymentService> logger)
        {
            _config = config;
            _publishEndpoint = publishEndpoint;
            _context = context;
            _logger = logger;

        }

        public async Task<string> CreateCheckoutSessionAsync(Order order, Guid customerId)
        {
            var domain = _config["AppUrl"] ?? "http://localhost:4200";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                SuccessUrl = $"{domain}/checkout/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/checkout/cancel",

             
                PaymentIntentData = new SessionPaymentIntentDataOptions
                {
                    TransferGroup = order.Id.ToString()
                },

                Metadata = new Dictionary<string, string>
                {
                    { "OrderId", order.Id.ToString() },
                    { "CustomerId", customerId.ToString() }
                },
                LineItems = new List<SessionLineItemOptions>()
            };

            foreach (var item in order.Items)
            {
                var lineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "brl",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.ProductName,
                            Description = TruncateDescription(item.Product?.Description),
                        },
                        UnitAmount = (long)(Math.Round(item.UnitPrice, 2) * 100)
                    },
                    Quantity = item.Quantity
                };

                if (!string.IsNullOrEmpty(item.ProductImage) && Uri.IsWellFormedUriString(item.ProductImage, UriKind.Absolute))
                {
                    lineItem.PriceData.ProductData.Images = new List<string> { item.ProductImage };
                }

                options.LineItems.Add(lineItem);
            }

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return session.Url;
        }

        public async Task HandleWebhookAsync(string json, string stripeSignature)
        {
            var webhookSecret = _config["Stripe:WebhookSecret"];

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);

                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Session;

                    if (!session.Metadata.ContainsKey("OrderId"))
                    {
                        _logger.LogError("Webhook recebido sem OrderId nos metadados.");
                        return;
                    }

                    var orderId = Guid.Parse(session.Metadata["OrderId"]);
                    var customerId = Guid.Parse(session.Metadata["CustomerId"]);

                    var order = await _context.Orders
                        .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Seller) 
                        .FirstOrDefaultAsync(o => o.Id == orderId);

                    if (order == null)
                    {
                        _logger.LogError($"Pedido {orderId} não encontrado no processamento do Webhook.");
                        return;
                    }

                    await ProcessSplitPayment(order);

                    var paymentEvent = new PaymentProcessedEvent
                    {
                        OrderId = orderId,
                        CustomerId = customerId,
                        StripeSessionId = session.Id,
                        PaymentIntentId = session.PaymentIntentId,
                        Amount = (decimal)(session.AmountTotal ?? 0) / 100m,
                        ProcessedAt = DateTime.UtcNow
                    };

                    await _publishEndpoint.Publish(paymentEvent);
                    _logger.LogInformation($"Pagamento processado com sucesso para o Pedido {orderId}");
                }
            }
            catch (StripeException e)
            {
                _logger.LogError($"Erro no Stripe Webhook: {e.Message}");
                throw; 
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro geral no Webhook: {ex.Message}");
            }
        }

        private async Task ProcessSplitPayment(Order order)
        {
            var transferService = new TransferService();

            var itemsBySeller = order.Items.GroupBy(i => i.Product.SellerId);

            foreach (var group in itemsBySeller)
            {
                var seller = group.First().Product.Seller;

                if (string.IsNullOrEmpty(seller.StripeAccountId))
                {
                    _logger.LogWarning($"[SPLIT FALHOU] Vendedor {seller.StoreName} ({seller.Id}) não tem StripeAccountId conectado. Dinheiro retido na plataforma.");
                    continue;
                }

                decimal sellerTotal = group.Sum(i => i.UnitPrice * i.Quantity);


                decimal commissionAmount = sellerTotal * (seller.CommissionRate / 100m);

                decimal amountToTransfer = sellerTotal - commissionAmount;

                if (amountToTransfer <= 0) continue;

                var transferOptions = new TransferCreateOptions
                {
                    Amount = (long)(Math.Round(amountToTransfer, 2) * 100),
                    Currency = "brl",
                    Destination = seller.StripeAccountId, 
                    TransferGroup = order.Id.ToString(),  
                    Metadata = new Dictionary<string, string>
                    {
                        { "OrderId", order.Id.ToString() },
                        { "SellerId", seller.Id.ToString() },
                        { "CommissionValue", commissionAmount.ToString("F2") }
                    }
                };

                try
                {
                    var transfer = await transferService.CreateAsync(transferOptions);
                    _logger.LogInformation($"Transferência de {amountToTransfer:C} realizada para {seller.StoreName}. ID: {transfer.Id}");

                }
                catch (StripeException ex)
                {
                    _logger.LogError($"Erro ao transferir para vendedor {seller.StoreName}: {ex.Message}");
                    // Aqui você poderia implementar uma tabela de "Falhas de Transferência" para retentar depois
                }
            }
        }

        private string TruncateDescription(string? description)
        {
            if (string.IsNullOrEmpty(description)) return "Produto Artesanal";
            return description.Length > 50 ? description.Substring(0, 47) + "..." : description;
        }
    }
}