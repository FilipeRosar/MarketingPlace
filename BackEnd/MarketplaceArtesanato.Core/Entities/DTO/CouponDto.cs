using System;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class CreateCouponDto
    {
        [Required]
        [StringLength(50)]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public CouponType Type { get; set; }

        [Required]
        public DiscountType DiscountType { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal DiscountValue { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaxDiscount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinOrderValue { get; set; } = 0;

        [Required]
        public CouponScope Scope { get; set; } = CouponScope.EntireOrder;

        public Guid? ProductId { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? SellerId { get; set; }

        [Range(0, 100)]
        public decimal? PlatformSharePercentage { get; set; }

        [Required]
        public DateTime ValidFrom { get; set; }

        [Required]
        public DateTime ValidUntil { get; set; }

        [Range(0, int.MaxValue)]
        public int UsageLimit { get; set; } = 0;

        [Range(1, int.MaxValue)]
        public int UsageLimitPerUser { get; set; } = 1;

        public bool IsActive { get; set; } = true;
        public bool PreventsCombination { get; set; } = true;
        public bool OnlyWithoutPromotion { get; set; } = false;
        public bool OnlyFirstPurchase { get; set; } = false;

        public Guid? AutomationRuleId { get; set; }
        public Guid? CreatorSellerId { get; set; }
    }

    public class UpdateCouponDto
    {
        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? DiscountValue { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaxDiscount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinOrderValue { get; set; }

        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidUntil { get; set; }

        [Range(0, int.MaxValue)]
        public int? UsageLimit { get; set; }

        [Range(1, int.MaxValue)]
        public int? UsageLimitPerUser { get; set; }

        public bool? IsActive { get; set; }
        public bool? PreventsCombination { get; set; }
        public bool? OnlyWithoutPromotion { get; set; }
        public bool? OnlyFirstPurchase { get; set; }
    }

    public class CouponDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public CouponType Type { get; set; }
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MaxDiscount { get; set; }
        public decimal MinOrderValue { get; set; }
        public CouponScope Scope { get; set; }
        public Guid? ProductId { get; set; }
        public Guid? CategoryId { get; set; }
        public Guid? SellerId { get; set; }
        public decimal? PlatformSharePercentage { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public int UsageLimit { get; set; }
        public int UsageCount { get; set; }
        public int UsageLimitPerUser { get; set; }
        public bool IsActive { get; set; }
        public bool PreventsCombination { get; set; }
        public bool OnlyWithoutPromotion { get; set; }
        public bool OnlyFirstPurchase { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? CreatorSellerId { get; set; }
        public string? CreatorSellerName { get; set; }
    }

    public class ApplyCouponRequest
    {
        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public string CouponCode { get; set; } = string.Empty;

        [Required]
        public decimal OrderTotal { get; set; }

        public List<Guid> ProductIds { get; set; } = new();
        public Guid? SellerId { get; set; }
    }

    public class ValidateCouponRequest
    {
        [Required]
        public string CouponCode { get; set; } = string.Empty;

        [Required]
        public decimal OrderTotal { get; set; }

        public List<Guid> ProductIds { get; set; } = new();
        public Guid? SellerId { get; set; }
    }

    public class CouponValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public CouponDto? Coupon { get; set; }
        public decimal DiscountAmount { get; set; }
        public DiscountPaidBy PaidBy { get; set; }
        public decimal? PlatformPays { get; set; }
        public decimal? SellerPays { get; set; }
    }
}
