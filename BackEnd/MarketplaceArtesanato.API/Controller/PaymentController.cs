using MarketplaceArtesanato.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/webhook")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IStripePaymentService _stripeService;

        public PaymentController(IStripePaymentService stripePayment)
        {
            _stripeService = stripePayment;
        }

        [HttpPost]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"];

            try
            {
                await _stripeService.HandleWebhookAsync(json, signature);
                return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine($"[WEBHOOK ERRO STRIPE] {e.Message}");
                return BadRequest($"Stripe Error: {e.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WEBHOOK ERRO INTERNO] {ex.Message}");
                return StatusCode(500, $"Internal Error: {ex.Message}");
            }
        }
    }
}
