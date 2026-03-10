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
        private readonly ICommissionCalculationService _commissionCalculationService;

        public StripePaymentService(
            IConfiguration config,
            IPublishEndpoint publishEndpoint,
            ArtesianDbContext context,
            ILogger<StripePaymentService> logger,
            ISellerSubscriptionService subscriptionService,
            ICommissionCalculationService commissionCalculationService)
        {
            _config = config;
            _publishEndpoint = publishEndpoint;
            _context = context;
            _logger = logger;
            _subscriptionService = subscriptionService;
            _commissionCalculationService = commissionCalculationService;

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
        // 🔔 WEBHOOK STRIPE (OBRIGATÓRIO)
        // =====================================================
        public async Task HandleWebhookAsync(string json, string stripeSignature)
        {
            var webhookSecret = _config["Stripe:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(webhookSecret))
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

            // Idempotência
            try
            {
                if (await _context.StripeEventLogs.AnyAsync(e => e.EventId == stripeEvent.Id))
                {
                    _logger.LogInformation("Webhook duplicado ignorado: {EventId}", stripeEvent.Id);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao verificar idempotência do webhook Stripe {EventId}", stripeEvent.Id);
            }

            try
            {
                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                    await HandleCheckoutCompletedAsync(stripeEvent);

                if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                    await HandlePaymentIntentSucceededAsync(stripeEvent);

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
            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(session.PaymentStatus, "no_payment_required", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var orderId = Guid.Parse(session.Metadata["OrderId"]);

            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Seller)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.Status == OrderStatus.Confirmed)
                return;

            await ConfirmOrderPaymentAsync(order, session.PaymentIntentId);

            _logger.LogInformation("Pedido {OrderId} confirmado com sucesso", orderId);
        }

        private async Task HandlePaymentIntentSucceededAsync(StripeEvent stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent?.Metadata == null) return;

            if (!paymentIntent.Metadata.TryGetValue("Type", out var type) || type != "order")
                return;

            if (!paymentIntent.Metadata.TryGetValue("OrderId", out var orderRaw))
                return;

            if (!Guid.TryParse(orderRaw, out var orderId))
                return;

            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Seller)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.Status == OrderStatus.Confirmed)
                return;

            await ConfirmOrderPaymentAsync(order, paymentIntent.Id);

            _logger.LogInformation("Pedido {OrderId} confirmado via payment_intent.succeeded", orderId);
        }

        private async Task ConfirmOrderPaymentAsync(Order order, string? paymentIntentId)
        {
            // Tentar processar split de pagamento ANTES de confirmar
            if (!string.IsNullOrWhiteSpace(paymentIntentId))
            {
                try
                {
                    await ProcessSplitPaymentAsync(order, paymentIntentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao processar split de pagamento do pedido {OrderId}. Revertendo status.", order.Id);
                    order.Status = OrderStatus.PaymentFailed;
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    throw; // Propagar erro para evitar confirmar pedido sem transferência
                }
            }

            // Só confirmar após split bem-sucedido
            order.Status = OrderStatus.Confirmed;
            order.StripePaymentIntentId = paymentIntentId;
            order.UpdatedAt = DateTime.UtcNow;

            foreach (var item in order.Items)
                item.Product.StockQuantity -= item.Quantity;

            await _context.SaveChangesAsync();

            await _publishEndpoint.Publish(new OrderPaidEvent
            {
                OrderId = order.Id,
                CustomerId = order.BuyerId,
                Total = order.TotalAmount,
                PaidAt = DateTime.UtcNow
            });
        }

        // =====================================================
        // 🔁 ASSINATURA
        // =====================================================
        private async Task ProcessSubscriptionAsync(Session session)
        {
            var metadata = session.Metadata ?? new Dictionary<string, string>();

            if (!metadata.TryGetValue("SellerId", out var sellerRaw))
            {
                sellerRaw = session.ClientReferenceId ?? string.Empty;
            }

            if (!metadata.TryGetValue("SellerPlan", out var planRaw))
            {
                _logger.LogWarning("Webhook de assinatura sem SellerPlan. SessionId={SessionId}", session.Id);
                return;
            }

            if (!Guid.TryParse(sellerRaw, out var sellerId))
            {
                _logger.LogWarning("Webhook de assinatura com SellerId invalido. SessionId={SessionId}", session.Id);
                return;
            }

            if (!Enum.TryParse<SellerPlan>(planRaw, out var plan))
            {
                _logger.LogWarning("Webhook de assinatura com plano invalido. SessionId={SessionId}", session.Id);
                return;
            }

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
                
                // Usar serviço unificado para calcular comissão
                var (sellerCommission, serviceFee, _) = 
                    await _commissionCalculationService.CalculateFeesAsync(gross, seller);
                
                var net = gross - sellerCommission;

                if (net <= 0) continue;

                var result = await transferService.CreateAsync(new TransferCreateOptions
                {
                    Amount = (long)(net * 100),
                    Currency = "brl",
                    Destination = seller.StripeAccountId,
                    SourceTransaction = paymentIntentId,
                    TransferGroup = order.Id.ToString()
                });

                _logger.LogInformation(
                    "Split de pagamento processado. OrderId={OrderId}, SellerId={SellerId}, Gross={Gross}, Commission={Commission}, Net={Net}, TransferId={TransferId}",
                    order.Id, seller.Id, gross, sellerCommission, net, result.Id);
            }
        }
    }
}
