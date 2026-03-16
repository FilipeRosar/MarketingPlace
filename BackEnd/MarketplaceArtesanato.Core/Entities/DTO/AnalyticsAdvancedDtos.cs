using System;
using System.Collections.Generic;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    // Métricas de Conversão
    public class ConversionMetricsDto
    {
        public decimal ConversionRate { get; set; }
        public int ClickCount { get; set; }
        public int PurchaseCount { get; set; }
        public int AbandonedCarts { get; set; }
        public decimal CartAbandonmentRate { get; set; }
        public decimal ConversionChangePercent { get; set; }
        public List<HourlyConversionDto> HourlyData { get; set; } = new();
    }

    public class HourlyConversionDto
    {
        public int Hour { get; set; }
        public decimal ConversionRate { get; set; }
        public int Clicks { get; set; }
        public int Purchases { get; set; }
    }

    // Métricas de ROI
    public class ROIMetricsDto
    {
        public decimal TotalInvestment { get; set; }
        public decimal TotalReturn { get; set; }
        public decimal ROIPercent { get; set; }
        public decimal NetProfit { get; set; }
        public decimal ProfitMargin { get; set; }
        public decimal ROIPeriodChange { get; set; }
        public List<ProductROIDto> TopProductsByROI { get; set; } = new();
    }

    public class ProductROIDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Cost { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public decimal ROIPercent { get; set; }
        public int UnitsSold { get; set; }
    }

    // Comparativo de Períodos
    public class PeriodComparisonAdvancedDto
    {
        public DateRangeDto CurrentPeriod { get; set; } = new();
        public DateRangeDto PreviousPeriod { get; set; } = new();

        public PeriodMetricDto Revenue { get; set; } = new();
        public PeriodMetricDto Orders { get; set; } = new();
        public PeriodMetricDto Customers { get; set; } = new();
        public PeriodMetricDto AOV { get; set; } = new();
        public PeriodMetricDto ConversionRate { get; set; } = new();

        public List<DailyTrendDto> DailyComparison { get; set; } = new();
    }

    public class DateRangeDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class PeriodMetricDto
    {
        public decimal Current { get; set; }
        public decimal Previous { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal ChangePercent { get; set; }
        public bool IsTrendingUp { get; set; }
    }

    public class DailyTrendDto
    {
        public DateTime Date { get; set; }
        public decimal CurrentRevenue { get; set; }
        public decimal PreviousRevenue { get; set; }
        public int CurrentOrders { get; set; }
        public int PreviousOrders { get; set; }
    }

    // Análise de Clientes
    public class CustomerCohortAnalysisDto
    {
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }
        public int RepeatCustomers { get; set; }
        public decimal RepeatCustomerRate { get; set; }
        public decimal AverageCustomerLTV { get; set; }
        public decimal CustomerRetentionRate { get; set; }
        public int ChurnedCustomers { get; set; }
        public decimal ChurnRate { get; set; }
        public List<CustomerCohortDto> Cohorts { get; set; } = new();
    }

    public class CustomerCohortDto
    {
        public string CohortName { get; set; } = string.Empty;
        public int CustomersCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageLTV { get; set; }
        public decimal RetentionRate { get; set; }
        public decimal ChurnRate { get; set; }
    }

    // Previsão de Vendas (Simples - Média Móvel)
    public class SalesForecatDto
    {
        public DateTime ForecastStart { get; set; }
        public DateTime ForecastEnd { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public decimal Confidence { get; set; }
        public List<ForecastPointDto> Points { get; set; } = new();
        public string Trend { get; set; } = string.Empty;
    }

    public class ForecastPointDto
    {
        public DateTime Date { get; set; }
        public decimal ForecastedRevenue { get; set; }
        public decimal LowBound { get; set; }
        public decimal HighBound { get; set; }
    }

    // Valor ao Longo da Vida (Lifetime Value)
    public class LifetimeValueAnalysisDto
    {
        public decimal AverageLTV { get; set; }
        public decimal MedianLTV { get; set; }
        public decimal MaxLTV { get; set; }
        public decimal MinLTV { get; set; }
        public int HighValueCustomers { get; set; }
        public int MediumValueCustomers { get; set; }
        public int LowValueCustomers { get; set; }
        public List<LTVSegmentDto> Segments { get; set; } = new();
    }

    public class LTVSegmentDto
    {
        public string SegmentName { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
        public decimal AverageLTV { get; set; }
        public decimal TotalContribution { get; set; }
        public decimal ContributionPercent { get; set; }
    }

    // Dashboard Completo de Analytics Avançado
    public class AdvancedAnalyticsDashboardDto
    {
        public Guid SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }

        // Resumo Executivo
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public decimal AOV { get; set; }

        // Métricas Principais
        public ConversionMetricsDto ConversionMetrics { get; set; } = new();
        public ROIMetricsDto ROIMetrics { get; set; } = new();
        public CustomerCohortAnalysisDto CustomerAnalysis { get; set; } = new();

        // Comparativos e Tendências
        public PeriodComparisonAdvancedDto PeriodComparison { get; set; } = new();
        public SalesForecatDto SalesForecast { get; set; } = new();
        public LifetimeValueAnalysisDto LifetimeValueAnalysis { get; set; } = new();

        // Produtos e Categorias
        public List<ProductPerformanceAdvancedDto> TopProducts { get; set; } = new();
        public List<CategoryPerformanceDto> CategoryPerformance { get; set; } = new();
    }

    public class ProductPerformanceAdvancedDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }
        public int ViewCount { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal ROIPercent { get; set; }
        public int Rating { get; set; }
        public DateTime LastSale { get; set; }
    }

    public class CategoryPerformanceDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Contribution { get; set; }
        public decimal ConversionRate { get; set; }
        public decimal AverageProductRevenue { get; set; }
    }

    // DTOs para Relatório em PDF/CSV
    public class AnalyticsExportDto
    {
        public Guid SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string ExportFormat { get; set; } = "PDF"; // PDF, CSV, Excel
        public DateTime GeneratedAt { get; set; }

        // Dados para exportar
        public AdvancedAnalyticsDashboardDto Analytics { get; set; } = new();
        public List<OrderDetailExportDto> Orders { get; set; } = new();
    }

    public class OrderDetailExportDto
    {
        public Guid OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal OrderValue { get; set; }
        public int ItemCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Products { get; set; } = new();
    }
}
