using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface ISellerSubscriptionService
    {
        Task<SellerSubscription> GetActiveSubscriptionAsync(Guid sellerId);
        Task<SellerSubscription> SubscribeAsync(Guid sellerId, SellerPlan plan);
        Task<SellerSubscription> ChangePlanAsync(Guid sellerId, SellerPlan newPlan);
        Task<string> CreateCheckoutSessionAsync(Guid sellerId, SellerPlan plan);
        Task CancelAsync(Guid sellerId);
    }
}
