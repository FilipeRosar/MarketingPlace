using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/webhook")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ArtesianDbContext _context;

        public WebhookController(
            IConfiguration config,
            ArtesianDbContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"];

            try
            {
                // Valida a assinatura do Stripe para garantir que o evento é legítimo
                var webhookSecret = _config["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);

                // Verifica se o evento é de sessão completada
                if (stripeEvent.Type == "checkout.session.completed")
                {
                    if (stripeEvent.Data.Object is Session session)
                    {
                        // Busca o ID do pedido nos metadados que enviamos no checkout
                        if (session.Metadata != null && session.Metadata.TryGetValue("OrderId", out var orderIdStr))
                        {
                            if (Guid.TryParse(orderIdStr, out var orderId))
                            {
                                await ProcessOrderPayment(orderId, session);
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine($"[WEBHOOK ERRO] {e.Message}");
                return BadRequest();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO INTERNO] {ex.Message}");
                return StatusCode(500);
            }
        }

        private async Task ProcessOrderPayment(Guid orderId, Session session)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null || order.Status == Core.Entities.Enums.OrderStatus.Paid) return;

            order.Status = Core.Entities.Enums.OrderStatus.Paid;
            order.StripePaymentIntentId = session.PaymentIntentId;
            order.UpdatedAt = DateTime.UtcNow;


            if (session.AmountTotal.HasValue)
            {
                order.TotalAmount = session.AmountTotal.Value / 100m; 
                // Opcional: atualizar o total se houver discrepância ou taxas dinâmicas
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"[WEBHOOK] Pedido {orderId} confirmado e pago!");
        }
    }
}