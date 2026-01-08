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

            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        }
        public async Task<string> CreateCheckoutSessionAsync(Order order, Guid customerId)
        {
            var domain = _config["AppUrl"] ?? "https://localhost:7113";

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                Mode = "payment",
                SuccessUrl = $"{domain}/checkout/success?session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/checkout/cancel",
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
                            Name = item.Product?.Name ?? "Produto Artesanal",
                            Description = item.Product?.Description?.Length > 100
                                ? item.Product.Description.Substring(0, 97) + "..."
                                : item.Product?.Description,
                            Images = !string.IsNullOrEmpty(item.ProductImage)
                                ? new List<string> { item.ProductImage }
                                : null
                        },
                        UnitAmount = (long)(item.UnitPrice * 100)
                    },
                    Quantity = item.Quantity
                };

                options.LineItems.Add(lineItem);
            }

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return session.Url;
        }


        public async Task HandleWebhookAsync(string json, string stripeSignature)
        {
            var webhookSecret = _config["Stripe:WebhookSecret"];
            if (string.IsNullOrEmpty(webhookSecret))
                throw new InvalidOperationException("Webhook secret não configurado.");

            StripeEvent stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Assinatura do webhook inválida.");
                throw;

            }

            // CORREÇÃO: use Stripe.Events (com E maiúsculo) e o nome exato da constante
            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted ||
                stripeEvent.Type == EventTypes.CheckoutSessionAsyncPaymentSucceeded)
            {
                {
                    var session = stripeEvent.Data.Object as Session;

                    if (session?.Metadata == null || !session.Metadata.ContainsKey("OrderId"))
                    {
                        _logger.LogWarning("Webhook recebido sem OrderId no metadata.");
                        return;
                    }

                    var orderId = Guid.Parse(session.Metadata["OrderId"]);
                    var customerId = Guid.Parse(session.Metadata["CustomerId"]);

                    var order = await _context.Orders
                        .Include(o => o.Items)
                            .ThenInclude(i => i.Product)
                                .ThenInclude(p => p.Seller)
                        .Include(o => o.Buyer)
                        .FirstOrDefaultAsync(o => o.Id == orderId);

                    if (order == null)
                    {
                        _logger.LogError("Pedido {OrderId} não encontrado no webhook.", orderId);
                        return;
                    }

                    if (order.Status == OrderStatus.Confirmed)
                    {
                        _logger.LogInformation("Pedido {OrderId} já processado.", orderId);
                        return;
                    }

                    order.Status = OrderStatus.Confirmed;
                    order.StripePaymentIntentId = session.PaymentIntentId;
                    order.UpdatedAt = DateTime.UtcNow;

                    foreach (var item in order.Items)
                    {
                        if (item.Product != null)
                        {
                            item.Product.StockQuantity -= item.Quantity;
                        }
                    }

                    await _context.SaveChangesAsync();

                    await ProcessSplitPaymentAsync(order, session.PaymentIntentId);

                    await _publishEndpoint.Publish(new OrderPaidEvent
                    {
                        OrderId = order.Id,
                        CustomerId = customerId,
                        Total = order.TotalAmount,
                        PaidAt = DateTime.UtcNow
                    });

                    _logger.LogInformation("Pagamento confirmado e split executado para pedido {OrderId}", orderId);
                }
            }
            else if (stripeEvent.Type == EventTypes.CheckoutSessionAsyncPaymentFailed)
            {
                var session = stripeEvent.Data.Object as Session;

                if (session?.Metadata == null || !session.Metadata.ContainsKey("OrderId"))
                {
                    _logger.LogWarning("Webhook async_payment_failed recebido sem OrderId no metadata.");
                    return;
                }

                var orderId = Guid.Parse(session.Metadata["OrderId"]);

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null)
                {
                    _logger.LogError("Pedido {OrderId} nao encontrado no webhook async_payment_failed.", orderId);
                    return;
                }

                if (order.Status == OrderStatus.Canceled)
                {
                    _logger.LogInformation("Pedido {OrderId} ja cancelado.", orderId);
                    return;
                }

                order.Status = OrderStatus.Canceled;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Pagamento falhou (boleto) para pedido {OrderId}", orderId);
            }
        }
    
        
        private async Task ProcessSplitPaymentAsync(Order order, string paymentIntentId)
        {
            var transferService = new TransferService();

            // Agrupa itens por vendedor
            var itemsBySeller = order.Items
                .Where(i => i.Product?.Seller != null)
                .GroupBy(i => i.Product.SellerId);

            foreach (var group in itemsBySeller)
            {
                var seller = group.First().Product.Seller;

                if (string.IsNullOrEmpty(seller.StripeAccountId))
                {
                    _logger.LogWarning("Vendedor {SellerName} (ID: {SellerId}) não tem conta Stripe conectada. Valor retido.", seller.StoreName, seller.Id);
                    continue;
                }

                decimal sellerGross = group.Sum(i => i.UnitPrice * i.Quantity);
                decimal commissionRate = seller.CommissionRate / 100m; // ex: 10% = 0.10m
                decimal commissionAmount = sellerGross * commissionRate;
                decimal netAmount = sellerGross - commissionAmount;

                if (netAmount <= 0)
                {
                    _logger.LogInformation("Valor líquido zero para vendedor {SellerName}. Transferência ignorada.", seller.StoreName);
                    continue;
                }

                long amountInCents = (long)Math.Round(netAmount * 100);

                var transferOptions = new TransferCreateOptions
                {
                    Amount = amountInCents,
                    Currency = "brl",
                    Destination = seller.StripeAccountId,
                    SourceTransaction = paymentIntentId, // Vincula à cobrança original
                    TransferGroup = order.Id.ToString(),
                    Description = $"Venda Mitra.ma - Pedido #{order.Id}",
                    Metadata = new Dictionary<string, string>
                    {
                        { "OrderId", order.Id.ToString() },
                        { "SellerId", seller.Id.ToString() },
                        { "GrossAmount", sellerGross.ToString("F2") },
                        { "Commission", commissionAmount.ToString("F2") },
                        { "NetAmount", netAmount.ToString("F2") }
                    }
                };

                try
                {
                    var transfer = await transferService.CreateAsync(transferOptions);
                    _logger.LogInformation("Transferência de R$ {NetAmount} realizada para {SellerName} (Transfer ID: {TransferId})",
                        netAmount, seller.StoreName, transfer.Id);
                }
                catch (StripeException ex)
                {
                    _logger.LogError(ex, "Falha ao transferir R$ {NetAmount} para vendedor {SellerName}", netAmount, seller.StoreName);
                    // Aqui você pode salvar em uma tabela de "transferências pendentes" para retry manual
                }
            }
        }
    }
}
