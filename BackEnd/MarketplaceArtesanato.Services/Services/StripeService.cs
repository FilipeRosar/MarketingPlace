using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class StripeService
    {
        private readonly string _secretKey;
        private readonly string _webhookSecret;

        public StripeService (IConfiguration configuration)
        {
            _secretKey = configuration["Stripe:SecretKey"];
            _webhookSecret = configuration["Stripe:WebhookSecret"];
            StripeConfiguration.ApiKey = _secretKey;
        }
    }
}
