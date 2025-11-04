using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Events;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MarketplaceArtesanato.Data;
using MarketplaceArtesanato.Data.Data;

namespace MarketplaceArtesanato.Infrastructure.Consumers
{
    public class PaymentConsumer : IConsumer<PaymentProcessedEvent>
    {
        private readonly ArtesianDbContext _context;
        private readonly ILogger<PaymentConsumer> _logger;

        public PaymentConsumer(ArtesianDbContext context, ILogger<PaymentConsumer> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
        {
            var evt = context.Message;

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == evt.OrderId);

            if (order != null)
            {
                order.StripeSessionId = evt.StripeSessionId;
                order.StripePaymentIntentId = evt.PaymentIntentId;
                order.Status = OrderStatus.Paid;
                order.UpdatedAt = DateTime.UtcNow;

                foreach (var item in order.Items)
                {
                    item.Product.StockQuantity -= item.Quantity;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Pagamento processado: Order {OrderId}", evt.OrderId);

                // TODO: Enviar email de confirmação
            }
        }
    }
}