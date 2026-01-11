using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class CartPriceResult
    {
        public decimal Subtotal { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal FinalTotal { get; set; }
        public List<PriceAdjustment> CartLevelAdjustments { get; set; }
        public Dictionary<Guid, ProductPriceResult> ItemPrices { get; set; }
    }
}
