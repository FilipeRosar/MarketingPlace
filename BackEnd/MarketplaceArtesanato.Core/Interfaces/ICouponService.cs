using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface ICouponService
    {
        // CRUD para coupons
        Task<CouponDto> CreateCouponAsync(CreateCouponDto dto);
        Task<CouponDto?> GetCouponByIdAsync(Guid id);
        Task<CouponDto?> GetCouponByCodeAsync(string code);
        Task<List<CouponDto>> GetAllCouponsAsync(CouponType? type = null, bool? activeOnly = true);
        Task<List<CouponDto>> GetSellerCouponsAsync(Guid sellerId);
        Task<CouponDto> UpdateCouponAsync(Guid id, UpdateCouponDto dto);
        Task DeleteCouponAsync(Guid id);

        // Validação e aplicação
        Task<CouponValidationResult> ValidateCouponAsync(
            string couponCode, 
            Guid userId, 
            decimal orderTotal, 
            List<Guid> productIds,
            Guid? sellerId = null);

        Task<CouponValidationResult> ApplyCouponAsync(
            Guid orderId, 
            string couponCode, 
            Guid userId, 
            decimal orderTotal, 
            List<Guid> productIds,
            Guid? sellerId = null);

        // Consultas
        Task<List<CouponDto>> GetActiveCouponsAsync();
        Task<List<CouponDto>> GetPlatformCouponsAsync();
        Task<CouponUsageDto> GetCouponUsageAsync(Guid couponId);
    }

    public class CouponUsageDto
    {
        public int TotalUses { get; set; }
        public int RemainingUses { get; set; }
        public decimal TotalDiscountGiven { get; set; }
        public List<CouponUseDetailDto> RecentUses { get; set; } = new();
    }

    public class CouponUseDetailDto
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public Guid OrderId { get; set; }
        public decimal DiscountApplied { get; set; }
        public DateTime UsedAt { get; set; }
    }
}
