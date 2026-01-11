using MarketplaceArtesanato.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class ProductPriceResult
    {
        public Guid ProductId { get; set; }
        public decimal BasePrice { get; set; }        
        public decimal FinalPrice { get; set; }       
        public decimal TotalDiscount { get; set; }    
        public List<PriceAdjustment> Adjustments { get; set; }  
        public bool HasAnyDiscount => Adjustments.Any();
    }
    public class PriceAdjustment
    {
        public PriceAdjustmentType Type { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
        public int Priority { get; set; }
    }
}
