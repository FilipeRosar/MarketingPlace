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

    // Enums for Coupon System
    public enum CouponType
    {
        Platform = 1,      // Criado pelo marketplace
        Seller = 2,         // Criado pelo seller
        Intelligent = 3,    // Automático por regras
        PlanBased = 4       // Desbloqueado por plano
    }

    public enum DiscountType
    {
        Percentage = 1,     // % de desconto
        Fixed = 2          // Valor fixo
    }

    public enum CouponScope
    {
        EntireOrder = 1,                // Aplica a todo pedido
        Product = 2,                    // Apenas um produto
        Category = 3,                   // Apenas uma categoria
        Seller = 4,                     // Apenas seller específico
        WithoutPromotion = 5            // Apenas produtos sem promoção
    }

    public enum DiscountPaidBy
    {
        Platform = 1,       // Marketplace paga
        Seller = 2,         // Seller paga
        Hybrid = 3          // Dividido entre ambos
    }

    public class Coupon : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CouponType Type { get; set; } = CouponType.Platform;
        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal MinOrderValue { get; set; } = 0;
        
        // Scope and targeting
        public CouponScope Scope { get; set; } = CouponScope.EntireOrder;
        public Guid? ProductId { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? SellerId { get; set; }
        
        // For hybrid discounts
        public decimal? PlatformSharePercentage { get; set; }
        
        // Validity period
        public DateTime ValidFrom { get; set; } = DateTime.UtcNow;
        public DateTime ValidUntil { get; set; } = DateTime.UtcNow.AddMonths(1);
        
        // Usage limits
        public int UsageLimit { get; set; } = 0; // 0 = unlimited
        public int UsageCount { get; set; } = 0;
        public int UsageLimitPerUser { get; set; } = 1;
        
        // Status
        public bool IsActive { get; set; } = true;
        
        // Advanced rules
        public bool PreventsCombination { get; set; } = true;
        public bool OnlyWithoutPromotion { get; set; } = false;
        public bool OnlyFirstPurchase { get; set; } = false;
        
        // System fields
        public Guid? AutomationRuleId { get; set; }
        public Guid? CreatorSellerId { get; set; } // For seller-created coupons
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();
    }

    public class CouponUsage : BaseEntity
    {
        public Guid CouponId { get; set; }
        public Guid UserId { get; set; }
        public Guid OrderId { get; set; }
        public decimal DiscountApplied { get; set; }
        
        // Track who paid the discount
        public DiscountPaidBy PaidBy { get; set; } = DiscountPaidBy.Platform;
        public decimal? PlatformPaid { get; set; }
        public decimal? SellerPaid { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public Coupon Coupon { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}

