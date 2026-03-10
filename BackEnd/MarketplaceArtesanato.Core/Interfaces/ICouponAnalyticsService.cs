using MarketplaceArtesanato.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface ICouponAnalyticsService
    {
        /// <summary>
        /// Calcula o ROI de um cupom específico
        /// </summary>
        Task<CouponROIDto> CalculateROIAsync(Guid couponId);

        /// <summary>
        /// Obtém estatísticas de cupons de um seller
        /// </summary>
        Task<SellerCouponStatsDto> GetSellerCouponStatsAsync(Guid sellerId);

        /// <summary>
        /// Obtém performance de um cupom em um período específico
        /// </summary>
        Task<CouponPerformanceDto> GetCouponPerformanceAsync(Guid couponId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Obtém comparação de desempenho entre cupons de um seller
        /// </summary>
        Task<List<CouponPerformanceComparisonDto>> GetSellerCouponsComparisonAsync(Guid sellerId, int topN = 10);

        /// <summary>
        /// Obtém analytics detalhadas para dashboard
        /// </summary>
        Task<CouponAnalyticsDashboardDto> GetCouponAnalyticsDashboardAsync(Guid sellerId);
    }

    // DTOs
    public class CouponROIDto
    {
        public Guid CouponId { get; set; }
        public string CouponCode { get; set; }
        public decimal TotalDiscountGiven { get; set; }
        public decimal EstimatedRevenueGenerated { get; set; }
        public decimal ROI { get; set; } // (Revenue - Discount) / Discount * 100
        public int TotalUsages { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal ConversionRate { get; set; } // Percentage
        public DateTime CalculatedAt { get; set; }
    }

    public class SellerCouponStatsDto
    {
        public Guid SellerId { get; set; }
        public int ActiveCoupons { get; set; }
        public int TotalCoupons { get; set; }
        public decimal TotalDiscountSpent { get; set; }
        public decimal TotalRevenueGenerated { get; set; }
        public decimal AverageROI { get; set; }
        public decimal ConversionRate { get; set; }
        public int TotalCouponUsages { get; set; }
        public List<CouponQuickStatsDto> TopCoupons { get; set; } = new();
    }

    public class CouponQuickStatsDto
    {
        public Guid CouponId { get; set; }
        public string Code { get; set; }
        public decimal DiscountValue { get; set; }
        public int Usages { get; set; }
        public decimal ROI { get; set; }
        public bool IsActive { get; set; }
    }

    public class CouponPerformanceDto
    {
        public Guid CouponId { get; set; }
        public string Code { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalImpressions { get; set; }
        public int TotalUsages { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalOrderValue { get; set; }
        public decimal ROI { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<DailyPerformanceDto> DailyData { get; set; } = new();
    }

    public class DailyPerformanceDto
    {
        public DateTime Date { get; set; }
        public int Impressions { get; set; }
        public int Usages { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal OrderValue { get; set; }
    }

    public class CouponPerformanceComparisonDto
    {
        public Guid CouponId { get; set; }
        public string Code { get; set; }
        public int Usages { get; set; }
        public decimal ROI { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal AvgOrderValue { get; set; }
        public int Rank { get; set; }
    }

    public class CouponAnalyticsDashboardDto
    {
        public decimal TotalSavedByCustomers { get; set; }
        public int ActiveCouponsCount { get; set; }
        public decimal AverageROI { get; set; }
        public decimal ConversionRate { get; set; }
        public List<CouponPerformanceComparisonDto> TopPerformers { get; set; } = new();
        public List<CouponPerformanceComparisonDto> BottomPerformers { get; set; } = new();
        public decimal MonthlyTrend { get; set; } // % change vs last month
    }
}
