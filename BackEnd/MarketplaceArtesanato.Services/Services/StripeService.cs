using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Events;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
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
            var domain = _config["AppUrl"] ?? "http://localhost:4200"; 

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" }, // Add "boleto", "pix" if configured in Stripe Dashboard
                LineItems = order.Items.Select(item => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "brl",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Name,
                            Description = item.Product.Description != null && item.Product.Description.Length > 50
                                ? item.Product.Description.Substring(0, 47) + "..."
                                : item.Product.Description,
                        },
                        UnitAmount = (long)(item.UnitPrice * 100) 
                    },
                    Quantity = item.Quantity
                }).ToList(),
                Mode = "payment",
                SuccessUrl = $"{domain}/#/orders?status=success&session_id={{CHECKOUT_SESSION_ID}}",
                CancelUrl = $"{domain}/#/cart?status=cancelled",
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
                var webhookSecret = _config["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(payload, signature, webhookSecret);

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Session;
                    if (session == null) return false;

                    if (session.Metadata.TryGetValue("orderId", out var orderIdStr) &&
                        session.Metadata.TryGetValue("customerId", out var customerIdStr))
                    {
                        var evt = new PaymentProcessedEvent
                        {
                            OrderId = Guid.Parse(orderIdStr),
                            CustomerId = Guid.Parse(customerIdStr),
                            Total = (decimal)(session.AmountTotal ?? 0) / 100m,
                            StripeSessionId = session.Id,
                            PaymentIntentId = session.PaymentIntentId,
                            ProcessedAt = DateTime.UtcNow
                        };

                        // Publish event to RabbitMQ so the Consumer can update the database
                        await _publishEndpoint.Publish(evt);
                        return true;
                    }
                }

                return false; 
            }
            catch (StripeException e)
            {
                Console.WriteLine($"Stripe Webhook Error: {e.Message}");
                throw; 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Webhook Error: {ex.Message}");
                return false;
            }
        }
    }
}