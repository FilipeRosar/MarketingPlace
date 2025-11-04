using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;  

namespace MarketplaceArtesanato.Infrastructure.Services
{
    public class StripePaymentService
    {
        private readonly IConfiguration _config;
        private readonly IPublishEndpoint _publishEndpoint;

        public StripePaymentService(IConfiguration config, IPublishEndpoint publishEndpoint)
        {
            _config = config;
            _publishEndpoint = publishEndpoint;
            StripeConfiguration.ApiKey = _config["Stripe:SecretKey"];
        }

        public async Task<string> CreateCheckoutSessionAsync(Order order)
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card", "boleto", "pix" },
                LineItems = order.Items.Select(item => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "brl",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name,
                            Description = item.Product.Description
                        },
                        UnitAmount = (long)(item.UnitPrice * 100)
                    },
                    Quantity = item.Quantity
                }).ToList(),
                Mode = "payment",
                SuccessUrl = _config["Stripe:SuccessUrl"] + "?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = _config["Stripe:CancelUrl"],
                Metadata = new Dictionary<string, string>
                {
                    { "orderId", order.Id.ToString() },
                    { "customerId", order.BuyerId.ToString() }
                }
            };

            var service = new SessionService(); 
            var session = await service.CreateAsync(options); 

            return session.Url;
        }

        public async Task<bool> HandleWebhookAsync(string payload, string signature)
        {
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    payload, signature, _config["Stripe:WebhookSecret"]);

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session == null) return false;

                    var orderId = Guid.Parse(session.Metadata["orderId"]);

                    var evt = new PaymentProcessedEvent
                    {
                        OrderId = orderId,
                        CustomerId = Guid.Parse(session.Metadata["customerId"]),
                        Total = (decimal)(session.AmountTotal / 100m),
                        StripeSessionId = session.Id,
                        PaymentIntentId = session.PaymentIntentId
                    };

                    await _publishEndpoint.Publish(evt);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Webhook error: {ex.Message}");
            }

            return false;
        }
    }
}