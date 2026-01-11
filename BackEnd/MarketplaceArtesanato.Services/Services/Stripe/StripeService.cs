using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using StripeEvent = Stripe.Event;

namespace MarketplaceArtesanato.Services.Services
{
    public class StripePaymentService : IStripePaymentService
    {
        private readonly IConfiguration _config;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ArtesianDbContext _context;
        private readonly ILogger<StripePaymentService> _logger;
        private readonly ISellerSubscriptionService _subscriptionService;

        public StripePaymentService(
            IConfiguration config,
            IPublishEndpoint publishEndpoint,
            ArtesianDbContext context,
            ILogger<StripePaymentService> logger,
            ISellerSubscriptionService subscriptionService)
        {
            _config = config;
            _publishEndpoint = publishEndpoint;
            _context = context;
            _logger = logger;
            _subscriptionService = subscriptionService;

            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        }

        // =====================================================
        // 🛒 CHECKOUT DE PEDIDO
        // =====================================================
        public async Task<string> CreateCheckoutSessionAsync(Order order, Guid customerId)
        {
            var domain = _config["AppUrl"] ?? "https://localhost:7113";

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                PaymentMethodTypes = new() { "card" },
                SuccessUrl = $"{domain}/checkout/success",
                CancelUrl = $"{domain}/checkout/cancel",
                Metadata = new Dictionary<string, string>
                {
                    { "Type", "order" },
                    { "OrderId", order.Id.ToString() },
                    { "CustomerId", customerId.ToString() }
                },
                LineItems = order.Items.Select(item => new SessionLineItemOptions
                {
                    Quantity = item.Quantity,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "brl",
                        UnitAmount = (long)(item.UnitPrice * 100),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product?.Name ?? "Produto Artesanal"
                        }
                    }
                }).ToList()
            };

            var session = await new SessionService().CreateAsync(options);
            return session.Url;
        }

        // =====================================================
        // 🔔 WEBHOOK STRIPE (IDEMPOTENTE)
        // =====================================================
        public async Task HandleWebhookAsync(string json, string stripeSignature)
        {
            var webhookSecret = _config["Stripe:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
                throw new InvalidOperationException("Stripe webhook secret não configurado.");

            StripeEvent stripeEvent;

            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Assinatura inválida do webhook Stripe");
                throw;
            }

            // 🔐 IDEMPOTÊNCIA
            if (await _context.StripeEventLogs.AnyAsync(e => e.EventId == stripeEvent.Id))
            {
                _logger.LogInformation("Webhook duplicado ignorado: {EventId}", stripeEvent.Id);
                return;
            }

            try
            {
                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    await HandleCheckoutCompletedAsync(stripeEvent);
                }

                _context.StripeEventLogs.Add(new StripeEventLog
                {
                    EventId = stripeEvent.Id,
                    EventType = stripeEvent.Type,
                    ProcessedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar webhook Stripe {EventId}", stripeEvent.Id);
                throw;
            }
        }

        // =====================================================
        // 📦 CHECKOUT FINALIZADO
        // =====================================================
        private async Task HandleCheckoutCompletedAsync(StripeEvent stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session?.Metadata == null) return;

            if (!session.Metadata.TryGetValue("Type", out var type))
                return;

            if (type == "order")
                await ProcessOrderPaymentAsync(session);

            if (type == "subscription")
                await ProcessSubscriptionAsync(session);
        }

        // =====================================================
        // 🧾 PEDIDO
        // =====================================================
        private async Task ProcessOrderPaymentAsync(Session session)
        {
            var orderId = Guid.Parse(session.Metadata["OrderId"]);
            var customerId = Guid.Parse(session.Metadata["CustomerId"]);

            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Seller)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.Status == OrderStatus.Confirmed)
                return;

            order.Status = OrderStatus.Confirmed;
            order.StripePaymentIntentId = session.PaymentIntentId;
            order.UpdatedAt = DateTime.UtcNow;

            foreach (var item in order.Items)
                item.Product.StockQuantity -= item.Quantity;

            await _context.SaveChangesAsync();
            await ProcessSplitPaymentAsync(order, session.PaymentIntentId);

            await _publishEndpoint.Publish(new OrderPaidEvent
            {
                OrderId = order.Id,
                CustomerId = customerId,
                Total = order.TotalAmount,
                PaidAt = DateTime.UtcNow
            });

            _logger.LogInformation("Pedido {OrderId} confirmado com sucesso", orderId);
        }

        // =====================================================
        // 🔁 ASSINATURA
        // =====================================================
        private async Task ProcessSubscriptionAsync(Session session)
        {
            if (!session.Metadata.TryGetValue("SellerId", out var sellerRaw) ||
                !session.Metadata.TryGetValue("SellerPlan", out var planRaw))
                return;

            if (!Guid.TryParse(sellerRaw, out var sellerId) ||
                !Enum.TryParse<SellerPlan>(planRaw, out var plan))
                return;

            await _subscriptionService.SubscribeAsync(sellerId, plan);

            _logger.LogInformation("Assinatura aplicada via Stripe: Seller={SellerId}, Plan={Plan}", sellerId, plan);
        }

        // =====================================================
        // 💸 SPLIT DE PAGAMENTO
        // =====================================================
        private async Task ProcessSplitPaymentAsync(Order order, string paymentIntentId)
        {
            var transferService = new TransferService();

            var groups = order.Items
                .Where(i => i.Product?.Seller != null)
                .GroupBy(i => i.Product.Seller);

            foreach (var group in groups)
            {
                var seller = group.Key;

                if (string.IsNullOrEmpty(seller.StripeAccountId))
                    continue;

                var gross = group.Sum(i => i.UnitPrice * i.Quantity);
                var commission = gross * (seller.CommissionRate / 100m);
                var net = gross - commission;

                if (net <= 0) continue;

                await transferService.CreateAsync(new TransferCreateOptions
                {
                    Amount = (long)(net * 100),
                    Currency = "brl",
                    Destination = seller.StripeAccountId,
                    SourceTransaction = paymentIntentId,
                    TransferGroup = order.Id.ToString()
                });
            }
        }
    }
}
