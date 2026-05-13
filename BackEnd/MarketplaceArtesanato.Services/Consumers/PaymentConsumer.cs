using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Data.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketplaceArtesanato.Infrastructure.Consumers;

public class PaymentConsumer : IConsumer<PaymentProcessedEvent>
{
    private readonly ArtesianDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint; 
    private readonly ILogger<PaymentConsumer> _logger;

    public PaymentConsumer(
        ArtesianDbContext context,
        IPublishEndpoint publishEndpoint,
        ILogger<PaymentConsumer> logger)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentProcessedEvent> context)
    {
        var evt = context.Message;

        try
        {
            var orderQuery = _context.Orders
                .Include(o => o.Items.Where(i => i.SellerId != Guid.Empty))
                    .ThenInclude(i => i.Product)
                .Include(o => o.Buyer);

            var order = evt.OrderId != Guid.Empty
                ? await orderQuery.FirstOrDefaultAsync(o => o.Id == evt.OrderId)
                : null;

            if (order == null && !string.IsNullOrWhiteSpace(evt.StripeSessionId))
            {
                order = await orderQuery.FirstOrDefaultAsync(o => o.StripeSessionId == evt.StripeSessionId);
            }

            if (order == null)
            {
                _logger.LogWarning("Pedido nao encontrado para sessao Stripe {SessionId} ou pedido {OrderId}", evt.StripeSessionId, evt.OrderId);
                return;
            }

            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
            {
                _logger.LogInformation("Pedido {OrderId} ja processado com status {Status}", order.Id, order.Status);
                return;
            }

            foreach (var item in order.Items)
            {
                if (item.Product == null)
                {
                    _logger.LogError("Produto n�o encontrado no item do pedido {OrderId}", order.Id);
                    return;
                }

                if (item.Product.StockQuantity < item.Quantity)
                {
                    _logger.LogError("Estoque insuficiente para {ProductName} (Pedido {OrderId})", item.Product.Name, order.Id);
                    await _publishEndpoint.Publish(new PaymentFailedEvent
                    {
                        OrderId = order.Id,
                        Reason = $"Estoque insuficiente: {item.Product.Name}"
                    });
                    return;
                }
            }

            // 4. ATUALIZA PEDIDO
            order.StripePaymentIntentId = evt.PaymentIntentId;
            order.Status = OrderStatus.Confirmed;
            order.UpdatedAt = DateTime.UtcNow;

            foreach (var item in order.Items)
            {
                item.Product.StockQuantity -= item.Quantity;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Pagamento confirmado: Pedido {OrderId} | Total: {Total:C}", order.Id, order.TotalAmount);

            await _publishEndpoint.Publish(new OrderPaidEvent
            {
                OrderId = order.Id,
                CustomerId = order.BuyerId,
                Total = order.TotalAmount,
                PaidAt = DateTime.UtcNow,
                SellerCommissions = order.SellerCommissions ?? new()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar pagamento para sess�o {SessionId}", evt.StripeSessionId);
            throw; // Para retry automatico
        }
    }
}
