// Infrastructure/Consumers/PaymentConsumer.cs
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
    private readonly IPublishEndpoint _publishEndpoint; // INJETADO!
    private readonly ILogger<PaymentConsumer> _logger;

    public PaymentConsumer(
        ArtesianDbContext context,
        IPublishEndpoint publishEndpoint, // OBRIGATÓRIO
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
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Buyer)
                .FirstOrDefaultAsync(o => o.StripeSessionId == evt.StripeSessionId);

            if (order == null)
            {
                _logger.LogWarning("Pedido não encontrado para sessão Stripe {SessionId}", evt.StripeSessionId);
                return;
            }

            if (order.Status == OrderStatus.Paid)
            {
                _logger.LogInformation("Pagamento já processado para pedido {OrderId}", order.Id);
                return;
            }

            foreach (var item in order.Items)
            {
                if (item.Product == null)
                {
                    _logger.LogError("Produto não encontrado no item do pedido {OrderId}", order.Id);
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
            order.Status = OrderStatus.Paid;
            order.UpdatedAt = DateTime.UtcNow;

            // 5. DECREMENTA ESTOQUE
            foreach (var item in order.Items)
            {
                item.Product.StockQuantity -= item.Quantity;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Pagamento confirmado: Pedido {OrderId} | Total: {Total:C}", order.Id, order.TotalAmount);

            // 6. PUBLICA EVENTO DE SUCESSO COM COMISSÕES
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
            _logger.LogError(ex, "Erro ao processar pagamento para sessão {SessionId}", evt.StripeSessionId);
            throw; // Para retry automático
        }
    }
}