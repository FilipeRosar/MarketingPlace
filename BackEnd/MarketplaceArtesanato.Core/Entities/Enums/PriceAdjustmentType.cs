using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.Enums
{
    public enum PriceAdjustmentType
    {
        ProductDiscount = 1,
        Promotion = 2,
        Campaign = 3,
        Coupon = 4,
        SellerPlan = 5,
        LoyaltyDiscount = 6,
        BulkDiscount = 7,
        SeasonalDiscount = 8,
        Custom = 99  
    }
}
