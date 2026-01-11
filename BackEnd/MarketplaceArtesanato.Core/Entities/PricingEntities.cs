using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities
{
    public class Promotion : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public Guid SellerId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
        public List<Guid> ProductIds { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public Seller Seller { get; set; } = null!;

    }

    public class Campaign : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DiscountPercentage { get; set; }
        public List<Guid> CategoryIds { get; set; } = new();
        public List<Guid> SellerIds { get; set; } = new();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class Coupon : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DiscountType { get; set; } = "Percentage"; 
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public decimal? MinPurchaseAmount { get; set; }
        public int? MaxUsesPerUser { get; set; }
        public int? MaxTotalUses { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }

        public ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();
    }
    public class CouponUsage : BaseEntity
    {
        public Guid CouponId { get; set; }
        public Guid UserId { get; set; }
        public Guid OrderId { get; set; }
        public decimal DiscountApplied { get; set; }
        public DateTime UsedAt { get; set; }

        public Coupon Coupon { get; set; } = null!;
    }

}
