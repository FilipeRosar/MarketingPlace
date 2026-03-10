using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ArtesianDbContext _context;
        private readonly ISettingsService _settingsService;

        public AnalyticsService(ArtesianDbContext context, ISettingsService settingsService)
        {
            _context = context;
            _settingsService = settingsService;
        }

        public async Task<PlatformAnalyticsDto> GetPlatformAnalyticsAsync()
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = monthStart.AddMonths(-1);

            // Total GMV e Orders
            var orders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Confirmed || 
                           o.Status == OrderStatus.Sent || 
                           o.Status == OrderStatus.Delivered)
                .Include(o => o.Items)
                .ToListAsync();

            var totalGMV = orders.SelectMany(o => o.Items).Sum(i => i.UnitPrice * i.Quantity);
            var totalOrders = orders.Count;

            // Usuários
            var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted);
            var totalSellers = await _context.Sellers.CountAsync(s => !s.IsDeleted && s.IsApproved);
            var totalProducts = await _context.Products.CountAsync(p => !p.IsDeleted);
            var newUsersThisMonth = await _context.Users.CountAsync(u => !u.IsDeleted && u.CreatedAt >= monthStart);

            // Pedidos deste mês
            var newOrdersThisMonth = await _context.Orders
                .CountAsync(o => o.CreatedAt >= monthStart && 
                           (o.Status == OrderStatus.Confirmed || 
                            o.Status == OrderStatus.Sent || 
                            o.Status == OrderStatus.Delivered));

            // Receita da plataforma
            var serviceFee = await _settingsService.GetServiceFeeAsync();
            var commissionRate = await _settingsService.GetCommissionRateAsync();
            var platformRevenue = totalGMV * (commissionRate / 100m) + (newOrdersThisMonth * serviceFee);

            // AOV e Conversão
            var aov = totalOrders > 0 ? totalGMV / totalOrders : 0;
            var uniqueCustomers = await _context.Orders
                .Where(o => o.Status == OrderStatus.Confirmed || 
                           o.Status == OrderStatus.Sent || 
                           o.Status == OrderStatus.Delivered)
                .Select(o => o.BuyerId)
                .Distinct()
                .CountAsync();
            var conversionRate = totalUsers > 0 ? ((decimal)uniqueCustomers / totalUsers) * 100 : 0;

            // Taxa de crescimento
            var lastMonthOrders = await _context.Orders
                .CountAsync(o => o.CreatedAt >= lastMonthStart && o.CreatedAt < monthStart &&
                           (o.Status == OrderStatus.Confirmed || 
                            o.Status == OrderStatus.Sent || 
                            o.Status == OrderStatus.Delivered));

            var growthRate = lastMonthOrders > 0 
                ? (((decimal)(newOrdersThisMonth - lastMonthOrders) / lastMonthOrders) * 100)
                : 0;

            return new PlatformAnalyticsDto
            {
                TotalGMV = totalGMV,
                TotalOrders = totalOrders,
                TotalUsers = totalUsers,
                TotalSellers = totalSellers,
                TotalProducts = totalProducts,
                PlatformRevenue = platformRevenue,
                AverageOrderValue = aov,
                ConversionRate = conversionRate,
                NewUsersThisMonth = newUsersThisMonth,
                NewOrdersThisMonth = newOrdersThisMonth,
                GrowthRate = growthRate
            };
        }

        public async Task<List<TopProductDto>> GetTopProductsAsync(int limit = 10)
        {
            var topProducts = await _context.OrderItems
                .AsNoTracking()
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Seller)
                        .ThenInclude(s => s.User)
                .Where(oi => !oi.Product.IsDeleted)
                .GroupBy(oi => oi.Product.Id)
                .OrderByDescending(g => g.Sum(x => x.UnitPrice * x.Quantity))
                .Take(limit)
                .Select(g => new
                {
                    Product = g.FirstOrDefault().Product,
                    TotalSales = g.Sum(x => x.UnitPrice * x.Quantity),
                    TotalQuantity = g.Sum(x => x.Quantity)
                })
                .ToListAsync();

            return topProducts.Select(x => new TopProductDto
            {
                ProductId = x.Product.Id,
                ProductName = x.Product.Name,
                SellerName = x.Product.Seller?.StoreName ?? x.Product.Seller?.User?.Name ?? "Unknown",
                TotalSales = x.TotalSales,
                TotalQuantitySold = (int)x.TotalQuantity,
                AverageRating = 0,
                TotalReviews = 0
            }).ToList();
        }

        public async Task<UserAnalyticsDto> GetUserAnalyticsAsync()
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted);
            var buyers = await _context.Users.CountAsync(u => !u.IsDeleted && u.Role == UserRole.Customer);
            var sellers = await _context.Users.CountAsync(u => !u.IsDeleted && u.Role == UserRole.Seller);
            var admins = await _context.Users.CountAsync(u => !u.IsDeleted && u.Role == UserRole.Admin);
            
            var newUsersThisMonth = await _context.Users
                .CountAsync(u => !u.IsDeleted && u.CreatedAt >= monthStart);

            // Usuários ativos (com orders ou consultas)
            var activeUsersThisMonth = await _context.Orders
                .Where(o => o.CreatedAt >= monthStart)
                .Select(o => o.BuyerId)
                .Distinct()
                .CountAsync();

            // AOV médio
            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.Status == OrderStatus.Confirmed || 
                           o.Status == OrderStatus.Sent || 
                           o.Status == OrderStatus.Delivered)
                .ToListAsync();

            var avgLifetimeValue = buyers > 0 
                ? orders.SelectMany(o => o.Items).Sum(i => i.UnitPrice * i.Quantity) / buyers
                : 0;

            return new UserAnalyticsDto
            {
                TotalUsers = totalUsers,
                Buyers = buyers,
                Sellers = sellers,
                Admins = admins,
                NewUsersThisMonth = newUsersThisMonth,
                ActiveUsersThisMonth = activeUsersThisMonth,
                AverageUserLifetimeValue = avgLifetimeValue
            };
        }

        public async Task<List<SalesPeriodDto>> GetSalesByPeriodAsync()
        {
            var orders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Confirmed || 
                           o.Status == OrderStatus.Sent || 
                           o.Status == OrderStatus.Delivered)
                .Include(o => o.Items)
                .Where(o => o.CreatedAt >= DateTime.UtcNow.AddMonths(-12))
                .ToListAsync();

            var grouped = orders
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .OrderByDescending(g => new { g.Key.Year, g.Key.Month })
                .Take(12)
                .Reverse()
                .Select(g =>
                {
                    var monthOrders = g.ToList();
                    var totalSales = monthOrders.SelectMany(o => o.Items).Sum(i => i.UnitPrice * i.Quantity);
                    return new SalesPeriodDto
                    {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        TotalSales = totalSales,
                        TotalOrders = monthOrders.Count,
                        AverageOrderValue = monthOrders.Count > 0 ? totalSales / monthOrders.Count : 0,
                        NewCustomers = monthOrders.Select(o => o.BuyerId).Distinct().Count()
                    };
                })
                .ToList();

            return grouped;
        }

        public async Task<List<CategoryDistributionDto>> GetCategoryDistributionAsync()
        {
            var totalSales = await _context.OrderItems
                .SumAsync(i => i.UnitPrice * i.Quantity);

            // Agrupar por ProductCategory enum
            var products = await _context.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .ToListAsync();

            var grouped = products
                .GroupBy(p => p.Category)
                .Select(g =>
                {
                    var categoryProducts = g.ToList();
                    return new
                    {
                        Category = g.Key,
                        ProductCount = categoryProducts.Count,
                        CategorySales = 0m // TODO: calcular vendas por categoria
                    };
                })
                .OrderByDescending(x => x.CategorySales)
                .ToList();

            return grouped.Select(x => new CategoryDistributionDto
            {
                CategoryId = Guid.Empty,
                CategoryName = x.Category.ToString(),
                ProductCount = x.ProductCount,
                TotalSales = x.CategorySales,
                Percentage = totalSales > 0 ? (x.CategorySales / totalSales) * 100 : 0
            }).ToList();
        }

        public async Task<PlatformHealthDto> GetPlatformHealthAsync()
        {
            var pendingSellers = await _context.Sellers.CountAsync(s => !s.IsApproved && !s.IsDeleted);
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing);
            var lowStockProducts = await _context.Products.CountAsync(p => !p.IsDeleted && p.StockQuantity < 5);
            var inactiveListings = await _context.Products.CountAsync(p => !p.IsDeleted && p.Status != ProductStatus.Active);

            // Calcular score (0-100)
            var maxPendingSellers = 100;
            var maxPendingOrders = 500;
            var maxLowStock = 100;
            var maxInactive = 200;

            var healthScore = 100 - (
                (Math.Min(pendingSellers, maxPendingSellers) / maxPendingSellers * 15) +
                (Math.Min(pendingOrders, maxPendingOrders) / maxPendingOrders * 30) +
                (Math.Min(lowStockProducts, maxLowStock) / maxLowStock * 25) +
                (Math.Min(inactiveListings, maxInactive) / maxInactive * 30)
            );

            return new PlatformHealthDto
            {
                PendingSellers = pendingSellers,
                PendingOrders = pendingOrders,
                LowStockProducts = lowStockProducts,
                InactiveListings = inactiveListings,
                PlatformHealthScore = Math.Max(0, healthScore)
            };
        }

        public async Task<List<CommissionReportItemResponse>> GetSellerPerformanceAsync()
        {
            var serviceFee = await _settingsService.GetServiceFeeAsync();
            var orders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Confirmed || 
                           o.Status == OrderStatus.Sent || 
                           o.Status == OrderStatus.Delivered)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Seller)
                            .ThenInclude(s => s.User)
                .ToListAsync();

            var report = new Dictionary<Guid, CommissionReportItemResponse>();

            foreach (var order in orders)
            {
                var orderTotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
                if (orderTotal <= 0) continue;

                var grouped = order.Items.GroupBy(i => i.Product.SellerId);
                foreach (var group in grouped)
                {
                    var seller = group.First().Product.Seller;
                    var sellerSales = group.Sum(i => i.UnitPrice * i.Quantity);
                    var commissionRate = seller.CommissionRate;
                    var commissionEarned = sellerSales * (commissionRate / 100m);
                    var serviceFeeShare = serviceFee * (sellerSales / orderTotal);

                    if (!report.TryGetValue(seller.Id, out var item))
                    {
                        item = new CommissionReportItemResponse
                        {
                            SellerId = seller.Id,
                            SellerName = seller.StoreName ?? seller.User?.Name ?? "Seller",
                            TotalSales = 0m,
                            CommissionEarned = 0m,
                            Rate = commissionRate
                        };
                        report[seller.Id] = item;
                    }

                    item.TotalSales += sellerSales;
                    item.CommissionEarned += commissionEarned + serviceFeeShare;
                    item.Rate = commissionRate;
                }
            }

            return report.Values
                .OrderByDescending(x => x.TotalSales)
                .ToList();
        }

        public async Task<Dictionary<string, int>> GetConversionFunnelAsync()
        {
            var now = DateTime.UtcNow;
            var lastMonth = now.AddMonths(-1);

            var totalVisitors = await _context.Users.CountAsync(u => !u.IsDeleted && u.CreatedAt >= lastMonth);
            var uniqueCartUsers = await _context.Carts.AsNoTracking()
                .Where(c => c.UpdatedAt >= lastMonth && c.Items.Count > 0)
                .Select(c => c.UserId)
                .Distinct()
                .CountAsync();
            var orderPlaced = await _context.Orders.CountAsync(o => o.CreatedAt >= lastMonth);
            var orderCompleted = await _context.Orders.CountAsync(o => 
                (o.Status == OrderStatus.Delivered || o.Status == OrderStatus.Sent) && o.CreatedAt >= lastMonth);

            return new Dictionary<string, int>
            {
                { "Visitors", totalVisitors },
                { "Cart", uniqueCartUsers },
                { "CheckedOut", orderPlaced },
                { "Completed", orderCompleted }
            };
        }
    }
}
