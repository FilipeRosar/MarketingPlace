using MarketplaceArtesanato.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IStripePaymentService
    {
        Task<string> CreateCheckoutSessionAsync(Order order, Guid customerId);
        Task HandleWebhookAsync(string json, string stripeSignature);
    }
}
