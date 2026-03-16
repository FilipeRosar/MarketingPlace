using MarketplaceArtesanato.Core.Entities.Models.Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface ISellerAnalyticsService
    {
        Task<AdvancedAnalyticsDto> GetAdvancedAnalyticsAsync(Guid sellerId);
        Task<PeriodComparisonDto> GetPeriodComparisonAsync(Guid sellerId, int days = 30);

        Task<CustomerAnalysisDto> GetCustomerAnalysisAsync(Guid sellerId);

        Task<List<ProductPerformanceDto>> GetProductPerformanceAsync(Guid sellerId);

        Task<List<TrendDataDto>> GetTrendsAsync(Guid sellerId, int days = 90);

        Task<List<HourlyRevenueDto>> GetHourlyRevenueDistributionAsync(Guid sellerId);

        Task<CouponEffectivenessDto> GetCouponEffectivenessAsync(Guid sellerId);

        Task<AIInsightsDto> GetAIInsightsAsync(Guid sellerId);

        Task<RevenueForecastDto> GetRevenueForecastAsync(Guid sellerId, int daysAhead = 30);
        Task<CustomerSegmentationDto> GetCustomerSegmentationAsync(Guid sellerId);
        Task<SeasonalAnalysisDto> GetSeasonalAnalysisAsync(Guid sellerId);
        Task<byte[]> ExportAnalyticsAsCSVAsync(Guid sellerId);
        Task<byte[]> ExportAnalyticsAsPDFAsync(Guid sellerId);
    }

    #region DTOs

    public class AdvancedAnalyticsDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal ConversionRate { get; set; }
        public List<TrendDataDto> RevenueChart { get; set; }
        public List<ProductPerformanceDto> TopProducts { get; set; }
        public CustomerAnalysisDto CustomerAnalysis { get; set; }
        public CouponEffectivenessDto CouponMetrics { get; set; }
    }

    public class PeriodComparisonDto
    {
        public decimal CurrentPeriodRevenue { get; set; }
        public decimal PreviousPeriodRevenue { get; set; }
        public decimal RevenueChangePercent { get; set; }
        public int CurrentPeriodOrders { get; set; }
        public int PreviousPeriodOrders { get; set; }
        public decimal OrderChangePercent { get; set; }
        public decimal CurrentAOV { get; set; }
        public decimal PreviousAOV { get; set; }
        public decimal AOVChangePercent { get; set; }
    }

    public class CustomerAnalysisDto
    {
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }
        public int RepeatCustomers { get; set; }
        public decimal RepeatCustomerRate { get; set; }
        public decimal AverageCustomerLifetimeValue { get; set; }
        public decimal CustomerRetentionRate { get; set; }
        public int ChurnedCustomers { get; set; }
    }

    public class ProductPerformanceDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
        public int ViewCount { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal Margin { get; set; }
        public int Rank { get; set; }
    }

    public class TrendDataDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public int Visitors { get; set; }
    }

    public class HourlyRevenueDto
    {
        public int Hour { get; set; }
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class CouponEffectivenessDto
    {
        public int ActiveCoupons { get; set; }
        public decimal TotalCustomerSavings { get; set; }
        public decimal AverageDiscount { get; set; }
        public decimal ROI { get; set; }
        public decimal ConversionLift { get; set; }
        public List<CouponMetricDto> TopCoupons { get; set; }
    }

    public class CouponMetricDto
    {
        public Guid CouponId { get; set; }
        public string Code { get; set; }
        public int UsageCount { get; set; }
        public decimal CustomerSavings { get; set; }
        public decimal ROI { get; set; }
    }

    public class AIInsightsDto
    {
        public string Summary { get; set; }
        public List<string> Recommendations { get; set; }
        public string BestSellingCategory { get; set; }
        public string WorstPerformingProduct { get; set; }
        public string OptimalPriceRecommendation { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class RevenueForecastDto
    {
        public List<ForecastPointDto> Forecast { get; set; }
        public decimal ExpectedTotalRevenue { get; set; }
        public decimal Confidence { get; set; }
        public DateTime ForecastPeriodStart { get; set; }
        public DateTime ForecastPeriodEnd { get; set; }
    }

    public class ForecastPointDto
    {
        public DateTime Date { get; set; }
        public decimal PredictedRevenue { get; set; }
        public decimal ConfidenceInterval { get; set; }
    }

    public class CustomerSegmentationDto
    {
        public List<SegmentDto> Segments { get; set; }
        public SegmentDto HighValueSegment { get; set; }
        public SegmentDto AtRiskSegment { get; set; }
        public SegmentDto ChurnedSegment { get; set; }
    }

    public class SegmentDto
    {
        public string Name { get; set; }
        public int CustomerCount { get; set; }
        public decimal AverageLifetimeValue { get; set; }
        public decimal ChurnRate { get; set; }
        public decimal PurchaseFrequency { get; set; }
    }

    public class SeasonalAnalysisDto
    {
        public List<MonthlyTrendDto> MonthlyData { get; set; }
        public string PeakSeason { get; set; }
        public string OffSeason { get; set; }
        public decimal SeasonalVariance { get; set; }
        public List<string> RecommendedStrategies { get; set; }
    }

    public class MonthlyTrendDto
    {
        public string Month { get; set; }
        public decimal AverageRevenue { get; set; }
        public int AverageOrders { get; set; }
        public decimal YearOverYearGrowth { get; set; }
    }

    #endregion
}
