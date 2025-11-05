using MarketplaceArtesanato.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/webhook")]
[ApiController]
public class WebhookController : ControllerBase
{
    private readonly StripePaymentService _stripeService;

    public WebhookController(StripePaymentService stripeService)
    {
        _stripeService = stripeService;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];

        var success = await _stripeService.HandleWebhookAsync(json, signature);
        return success ? Ok() : BadRequest();
    }
}