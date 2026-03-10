using System;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    /// <summary>
    /// Analytics geral da plataforma
    /// </summary>
    public class PlatformAnalyticsDto
    {
        public decimal TotalGMV { get; set; }
        public int TotalOrders { get; set; }
        public int TotalUsers { get; set; }
        public int TotalSellers { get; set; }
        public int TotalProducts { get; set; }
        public decimal PlatformRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal ConversionRate { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int NewOrdersThisMonth { get; set; }
        public decimal GrowthRate { get; set; }
    }

    /// <summary>
    /// Top produtos por vendas
    /// </summary>
    public class TopProductDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string SellerName { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
    }

    /// <summary>
    /// Estatísticas de usuários
    /// </summary>
    public class UserAnalyticsDto
    {
        public int TotalUsers { get; set; }
        public int Buyers { get; set; }
        public int Sellers { get; set; }
        public int Admins { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int ActiveUsersThisMonth { get; set; }
        public decimal AverageUserLifetimeValue { get; set; }
    }

    /// <summary>
    /// Dados de vendas por período
    /// </summary>
    public class SalesPeriodDto
    {
        public string Period { get; set; } // "2024-01", "Week 1", etc
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int NewCustomers { get; set; }
    }

    /// <summary>
    /// Distribuição de produtos por categoria
    /// </summary>
    public class CategoryDistributionDto
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int ProductCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Saúde da plataforma
    /// </summary>
    public class PlatformHealthDto
    {
        public int PendingSellers { get; set; }
        public int PendingOrders { get; set; }
        public int LowStockProducts { get; set; }
        public int InactiveListings { get; set; }
        public decimal PlatformHealthScore { get; set; } // 0-100
    }
}
