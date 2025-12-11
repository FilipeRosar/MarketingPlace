using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace MarketplaceArtesanato.Infrastructure.Consumers
{
    public class CheckoutConsumer : IConsumer<CheckoutInitiatedEvent>
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CheckoutConsumer> _logger;

        public CheckoutConsumer(
            ICartService cartService,
            ILogger<CheckoutConsumer> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CheckoutInitiatedEvent> context)
        {
            var evt = context.Message;

            try
            {
                _logger.LogInformation($"[EVENTO] Checkout iniciado para cliente {evt.CustomerId}. Sessão: {evt.StripeSessionId}");

                await _cartService.ClearCartAsync(evt.CustomerId);

                _logger.LogInformation($"Carrinho limpo com sucesso para cliente {evt.CustomerId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro no processamento secundário do checkout para cliente {CustomerId}", evt.CustomerId);
            }
        }
    }
}