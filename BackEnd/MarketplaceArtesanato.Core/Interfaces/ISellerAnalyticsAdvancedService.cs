using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface ISellerAnalyticsAdvancedService
    {
        Task<AdvancedAnalyticsDashboardDto> GetAdvancedDashboardAsync(Guid sellerId, int days = 30);

        Task<ConversionMetricsDto> GetConversionMetricsAsync(Guid sellerId, List<Order> orders, List<User> customers);
        Task<ROIMetricsDto> GetROIMetricsAsync(Guid sellerId, List<Order> orders, List<Product> products);
        Task<CustomerCohortAnalysisDto> GetCustomerCohortAnalysisAsync(Guid sellerId, List<User> customers, List<Order> orders);
        Task<PeriodComparisonAdvancedDto> GetPeriodComparisonAsync(Guid sellerId, int days = 30);
        Task<SalesForecatDto> GenerateSalesForecastAsync(Guid sellerId, int historicalDays = 30, int forecastDays = 30);
        Task<LifetimeValueAnalysisDto> GetLifetimeValueAnalysisAsync(Guid sellerId, List<User> customers, List<Order> orders);
        Task<AnalyticsExportDto> GenerateExportAsync(Guid sellerId, DateTime periodStart, DateTime periodEnd, string format = "PDF");
    }
}
