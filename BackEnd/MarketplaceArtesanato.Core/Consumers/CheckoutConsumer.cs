using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace MarketplaceArtesanato.Infrastructure.Consumers;

public class CheckoutConsumer : IConsumer<CheckoutInitiatedEvent>
{
    private readonly IOrderService _orderService;
    private readonly ICartService _cartService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CheckoutConsumer> _logger;

    public CheckoutConsumer(
        IOrderService orderService,
        ICartService cartService,
        IPublishEndpoint publishEndpoint,
        ILogger<CheckoutConsumer> logger)
    {
        _orderService = orderService;
        _cartService = cartService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CheckoutInitiatedEvent> context)
    {
        var evt = context.Message;

        try
        {
            _logger.LogInformation("Processando checkout para cliente {CustomerId}", evt.CustomerId);

            var orderDto = await _orderService.CreateFromCartAsync(evt.CustomerId, new CreateCheckoutDto());

            await _publishEndpoint.Publish(new OrderCreatedEvent
            {
                OrderId = orderDto.Id,
                CustomerId = evt.CustomerId,
                Total = orderDto.Total
            });

            await _cartService.ClearCartAsync(evt.CustomerId);

            _logger.LogInformation("Checkout concluído: Pedido {OrderId}", orderDto.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no checkout para cliente {CustomerId}", evt.CustomerId);

            await _publishEndpoint.Publish(new CheckoutFailedEvent
            {
                CustomerId = evt.CustomerId,
                Error = ex.Message
            });

            throw; 
        }
    }
}