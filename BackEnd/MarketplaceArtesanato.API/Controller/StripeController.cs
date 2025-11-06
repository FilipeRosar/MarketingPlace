using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace MarketplaceArtesanato.API.Controllers;

[Authorize(Roles = "Customer")]
[Route("api/stripe")]
[ApiController]
public class StripeController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IConfiguration _config;

    public StripeController(
        ICartService cartService,
        IPublishEndpoint publishEndpoint,
        IConfiguration config)
    {
        _cartService = cartService;
        _publishEndpoint = publishEndpoint;
        _config = config;
    }

    [HttpPost("create-session")]
    public async Task<IActionResult> CreateCheckoutSession()
    {
        var customerId = User.GetUserId();
        var cart = await _cartService.GetCartAsync(customerId);

        if (!cart.Items.Any())
            return BadRequest("Carrinho vazio");

        var domain = _config["AppUrl"] ?? "https://localhost:7113";
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = cart.Items.Select(i => new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "brl",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = i.ProductName,
                        Images = new List<string> { i.ProductImage }
                    },
                    UnitAmount = (long)(i.Price * 100) 
                },
                Quantity = i.Quantity
            }).ToList(),
            Mode = "payment",
            SuccessUrl = $"{domain}/checkout/success?session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{domain}/checkout/cancel",
            Metadata = new Dictionary<string, string>
            {
                { "CustomerId", customerId.ToString() }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return Ok(new { sessionId = session.Id, url = session.Url });
    }

   
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var stripeSignature = Request.Headers["Stripe-Signature"];
        var webhookSecret = _config["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(webhookSecret))
            return BadRequest("Webhook secret não configurado");

        Stripe.Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
        }
        catch (Exception ex)
        {
            return BadRequest($"Webhook signature inválida: {ex.Message}");
        }

        if (stripeEvent.Type == "checkout.session.completed")
        {
            var session = stripeEvent.Data.Object as Session;
            var customerId = Guid.Parse(session!.Metadata["CustomerId"]);

            await _publishEndpoint.Publish(new PaymentProcessedEvent
            {
                OrderId = Guid.Parse(session.Metadata.GetValueOrDefault("OrderId", Guid.NewGuid().ToString())),
                StripeSessionId = session.Id,
                PaymentIntentId = session.PaymentIntentId,
                Amount = session.AmountTotal / 100m,
                CustomerId = customerId
            });
        }

        return Ok();
    }
}