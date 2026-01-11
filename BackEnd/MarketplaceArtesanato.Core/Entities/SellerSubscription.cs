using MarketplaceArtesanato.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities
{
    public class SellerSubscription : BaseEntity
    {
        [Required]
        public Guid SellerId { get; set; }
        public Seller Seller { get; set; } = null!;

        [Required]
        public SellerPlan Plan { get; set; } = SellerPlan.Basic;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal CommissionRate { get; set; }
        public bool CanHighlightProducts { get; set; }
        public decimal MonthlyPrice { get; set; }
        public int HighlightLimit { get; set; }
        public bool HasVerifiedBadge { get; set; }
        public bool HasAdvancedAnalytics { get; set; }
        public bool HasPrioritySupport { get; set; }
    }
}
