using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Services.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe; 

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/webhook")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly StripePaymentService _stripeService;
        private readonly ArtesianDbContext _context;

        public WebhookController(
            IConfiguration config,
            StripePaymentService stripeService,
            ArtesianDbContext context)
        {
            _config = config;
            _stripeService = stripeService;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            var signature = Request.Headers["Stripe-Signature"];

            try
            {
                var webhookSecret = _config["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

                    if (session != null && session.Metadata.TryGetValue("OrderId", out var orderIdStr)) // Era OrderId no metadata
                    {
                        if (Guid.TryParse(orderIdStr, out var orderId))
                        {
                            var order = await _context.Orders.FindAsync(orderId);
                            if (order != null)
                            {
                                // ATUALIZA O STATUS DO PEDIDO
                                order.Status = Core.Entities.Enums.OrderStatus.Paid;
                                order.StripePaymentIntentId = session.PaymentIntentId;
                                order.UpdatedAt = DateTime.UtcNow;

                                await _context.SaveChangesAsync();

                                Console.WriteLine($"[WEBHOOK] Pedido {orderId} pago com sucesso!");
                            }
                        }
                    }
                }

                // Retorna 200 OK para o Stripe saber que recebemos
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
    }
}