using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using MarketplaceArtesanato.Core.Interfaces;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/webhook")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly ArtesianDbContext _context;
        private readonly IStripePaymentService _stripeService;

        public WebhookController(
            IConfiguration config,
            ArtesianDbContext context,
            IStripePaymentService stripePayment)
        {
            _config = config;
            _context = context;
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