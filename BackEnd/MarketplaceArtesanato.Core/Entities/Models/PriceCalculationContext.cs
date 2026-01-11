using MarketplaceArtesanato.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Data.Data
{
    public class PriceCalculationContext
    {
        public Product Product { get; set; } = null!;
        public Guid? UserId { get; set; }
        public string? CouponCode { get; set; }
        public decimal CurrentPrice { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
