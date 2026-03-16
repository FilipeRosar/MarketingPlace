using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class SellerAnalyticsService : ISellerAnalyticsService
    {
        private readonly ArtesianDbContext _context;

        public SellerAnalyticsService(ArtesianDbContext context)
        {
            _context = context;
        }

        public async Task<AdvancedAnalyticsDto> GetAdvancedAnalyticsAsync(Guid sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            if (seller.Subscription?.Plan == SellerPlan.Basic)
                throw new UnauthorizedAccessException("Apenas vendedores Pro e Premium podem acessar analytics avançado.");

            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => i.SellerId == sellerId) 
                    && o.CreatedAt >= thirtyDaysAgo
                    && o.BuyerId != Guid.Empty)  // Filter out null BuyerId
                .ToListAsync();

            var totalRevenue = orders.SelectMany(o => o.Items).Where(i => i.SellerId == sellerId).Sum(i => i.Subtotal);
            var totalOrders = orders.Count;
            var distinctCustomers = orders.Select(o => o.BuyerId).Distinct().Count();
            var avgOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            var productPerformance = await GetProductPerformanceAsync(sellerId);
            var topProducts = productPerformance.OrderByDescending(p => p.Revenue).Take(5).ToList();

            var trends = await GetTrendsAsync(sellerId, 30);
            var conversionRate = CalculateConversionRate(distinctCustomers, 100);

            return new AdvancedAnalyticsDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                TotalCustomers = distinctCustomers,
                AverageOrderValue = avgOrderValue,
                ConversionRate = conversionRate,
                RevenueChart = trends,
                TopProducts = topProducts,
                CustomerAnalysis = await GetCustomerAnalysisAsync(sellerId),
                CouponMetrics = await GetCouponEffectivenessAsync(sellerId)
            };
        }

        public async Task<PeriodComparisonDto> GetPeriodComparisonAsync(Guid sellerId, int days = 30)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            if (seller.Subscription?.Plan == SellerPlan.Basic)
                throw new UnauthorizedAccessException("Apenas vendedores Pro e Premium podem acessar analytics avançado.");

            var now = DateTime.UtcNow;
            var currentStart = now.AddDays(-days);
            var previousStart = currentStart.AddDays(-days);

            var currentOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => i.SellerId == sellerId) 
                    && o.CreatedAt >= currentStart 
                    && o.CreatedAt <= now
                    && o.BuyerId != Guid.Empty)  // Filter out null BuyerId
                .ToListAsync();

            var previousOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => i.SellerId == sellerId) 
                    && o.CreatedAt >= previousStart 
                    && o.CreatedAt < currentStart
                    && o.BuyerId != Guid.Empty)  // Filter out null BuyerId
                .ToListAsync();

            var currentRevenue = currentOrders.SelectMany(o => o.Items).Where(i => i.SellerId == sellerId).Sum(i => i.Subtotal);
            var previousRevenue = previousOrders.SelectMany(o => o.Items).Where(i => i.SellerId == sellerId).Sum(i => i.Subtotal);

            var currentAOV = currentOrders.Count > 0 ? currentRevenue / currentOrders.Count : 0;
            var previousAOV = previousOrders.Count > 0 ? previousRevenue / previousOrders.Count : 0;

            return new PeriodComparisonDto
            {
                CurrentPeriodRevenue = currentRevenue,
                PreviousPeriodRevenue = previousRevenue,
                RevenueChangePercent = previousRevenue > 0 ? ((currentRevenue - previousRevenue) / previousRevenue) * 100 : 0,
                CurrentPeriodOrders = currentOrders.Count,
                PreviousPeriodOrders = previousOrders.Count,
                OrderChangePercent = previousOrders.Count > 0 ? ((currentOrders.Count - previousOrders.Count) / (decimal)previousOrders.Count) * 100 : 0,
                CurrentAOV = currentAOV,
                PreviousAOV = previousAOV,
                AOVChangePercent = previousAOV > 0 ? ((currentAOV - previousAOV) / previousAOV) * 100 : 0
            };
        }

        public async Task<CustomerAnalysisDto> GetCustomerAnalysisAsync(Guid sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            if (seller.Subscription?.Plan == SellerPlan.Basic)
                throw new UnauthorizedAccessException("Apenas vendedores Pro e Premium podem acessar analytics avançado.");

            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            var sixtyDaysAgo = now.AddDays(-60);

            var allOrders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => i.SellerId == sellerId)
                    && o.BuyerId != null
                    && o.BuyerId != Guid.Empty)
                .ToListAsync();

            var newOrders = allOrders.Where(o => o.CreatedAt >= thirtyDaysAgo).ToList();
            var newCustomers = newOrders.Select(o => o.BuyerId).Distinct().Count();

            var allCustomers = allOrders.Select(o => o.BuyerId).Distinct().Count();
            var repeatCustomers = allOrders
                .GroupBy(o => o.BuyerId)
                .Where(g => g.Count() > 1)
                .Count();

            var totalRevenue = allOrders.Sum(o => o.Items.Where(i => i.SellerId == sellerId).Sum(i => i.Subtotal));
            var avgLifetimeValue = allCustomers > 0 ? totalRevenue / allCustomers : 0;

            var previousPeriodCustomers = allOrders.Where(o => o.CreatedAt < sixtyDaysAgo && o.CreatedAt >= thirtyDaysAgo).Select(o => o.BuyerId).Distinct().Count();
            var churnedCustomers = previousPeriodCustomers > 0 ? (previousPeriodCustomers - newCustomers) : 0;

            return new CustomerAnalysisDto
            {
                TotalCustomers = allCustomers,
                NewCustomers = newCustomers,
                RepeatCustomers = repeatCustomers,
                RepeatCustomerRate = allCustomers > 0 ? (repeatCustomers / (decimal)allCustomers) * 100 : 0,
                AverageCustomerLifetimeValue = avgLifetimeValue,
                CustomerRetentionRate = previousPeriodCustomers > 0 ? ((previousPeriodCustomers - churnedCustomers) / (decimal)previousPeriodCustomers) * 100 : 0,
                ChurnedCustomers = churnedCustomers
            };
        }

        public async Task<List<ProductPerformanceDto>> GetProductPerformanceAsync(Guid sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            if (seller.Subscription?.Plan == SellerPlan.Basic)
                throw new UnauthorizedAccessException("Apenas vendedores Pro e Premium podem acessar analytics avançado.");

            var products = await _context.Products
                .AsNoTracking()
                .Where(p => p.SellerId == sellerId && !p.IsDeleted)
                .ToListAsync();

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => i.SellerId == sellerId))
                .ToListAsync();

            var result = new List<ProductPerformanceDto>();
            var rank = 1;

            foreach (var product in products)
            {
                var productOrders = orders.Where(o => o.Items.Any(i => i.ProductId == product.Id && i.SellerId == sellerId)).ToList();
                var salesCount = productOrders.SelectMany(o => o.Items).Where(i => i.ProductId == product.Id && i.SellerId == sellerId).Sum(i => i.Quantity);
                var revenue = productOrders.SelectMany(o => o.Items).Where(i => i.ProductId == product.Id && i.SellerId == sellerId).Sum(i => i.Subtotal);
                var viewCount = 0;
                var conversionRate = viewCount > 0 ? (salesCount / (decimal)viewCount) * 100 : 0;

                result.Add(new ProductPerformanceDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    SalesCount = salesCount,
                    Revenue = revenue,
                    ViewCount = viewCount,
                    ConversionRate = conversionRate,
                    Margin = product.Price > 0 ? ((product.Price - product.Price * 0.2m) / product.Price) * 100 : 0,
                    Rank = rank++
                });
            }

            return result.OrderByDescending(p => p.Revenue).ToList();
        }

        public async Task<List<TrendDataDto>> GetTrendsAsync(Guid sellerId, int days = 90)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            var startDate = DateTime.UtcNow.AddDays(-days);

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => i.SellerId == sellerId) && o.CreatedAt >= startDate)
                .ToListAsync();

            var trends = new List<TrendDataDto>();

            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i).Date;
                var dayOrders = orders.Where(o => o.CreatedAt.Date == date).ToList();
                var revenue = dayOrders.SelectMany(o => o.Items).Where(i => i.SellerId == sellerId).Sum(i => i.Subtotal);

                trends.Add(new TrendDataDto
                {
                    Date = date,
                    Revenue = revenue,
                    Orders = dayOrders.Count,
                    Visitors = 0
                });
            }

            return trends;
        }

        public async Task<List<HourlyRevenueDto>> GetHourlyRevenueDistributionAsync(Guid sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            if (seller.Subscription?.Plan == SellerPlan.Basic)
                throw new UnauthorizedAccessException("Apenas vendedores Pro e Premium podem acessar analytics avançado.");

            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => i.SellerId == sellerId) && o.CreatedAt >= sevenDaysAgo)
                .ToListAsync();

            var result = new List<HourlyRevenueDto>();

            for (int hour = 0; hour < 24; hour++)
            {
                var hourOrders = orders.Where(o => o.CreatedAt.Hour == hour).ToList();
                var revenue = hourOrders.SelectMany(o => o.Items).Where(i => i.SellerId == sellerId).Sum(i => i.Subtotal);
                var orderCount = hourOrders.Count;

                result.Add(new HourlyRevenueDto
                {
                    Hour = hour,
                    Revenue = revenue,
                    Orders = orderCount,
                    AverageOrderValue = orderCount > 0 ? revenue / orderCount : 0
                });
            }

            return result;
        }

        public async Task<CouponEffectivenessDto> GetCouponEffectivenessAsync(Guid sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            var coupons = await _context.Coupons
                .AsNoTracking()
                .Include(c => c.Usages)
                .Where(c => c.SellerId == sellerId && !c.IsDeleted)
                .ToListAsync();

            var activeCoupons = coupons.Where(c => c.IsActive && c.ValidUntil > DateTime.UtcNow).Count();
            var totalSavings = coupons.SelectMany(c => c.Usages).Sum(u => u.DiscountApplied);
            var avgDiscount = coupons.Count > 0 ? totalSavings / coupons.Count : 0;

            var topCoupons = coupons
                .Select(c => new CouponMetricDto
                {
                    CouponId = c.Id,
                    Code = c.Code,
                    UsageCount = c.Usages.Count,
                    CustomerSavings = c.Usages.Sum(u => u.DiscountApplied),
                    ROI = c.UsageLimit > 0 ? (c.Usages.Count / (decimal)c.UsageLimit) * 100 : 0
                })
                .OrderByDescending(c => c.CustomerSavings)
                .Take(5)
                .ToList();

            return new CouponEffectivenessDto
            {
                ActiveCoupons = activeCoupons,
                TotalCustomerSavings = totalSavings,
                AverageDiscount = avgDiscount,
                ROI = 0,
                ConversionLift = 0,
                TopCoupons = topCoupons
            };
        }

        public async Task<AIInsightsDto> GetAIInsightsAsync(Guid sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            if (seller.Subscription?.Plan != SellerPlan.Premium)
                throw new UnauthorizedAccessException("Apenas vendedores Premium podem acessar insights com IA.");

            var analytics = await GetAdvancedAnalyticsAsync(sellerId);
            var productPerformance = await GetProductPerformanceAsync(sellerId);
            var worstProduct = productPerformance.OrderBy(p => p.Revenue).FirstOrDefault();

            var insights = new AIInsightsDto
            {
                Summary = $"Seu desempenho foi forte com R$ {analytics.TotalRevenue:F2} em receita e {analytics.TotalOrders} pedidos.",
                Recommendations = new List<string>
                {
                    "Aumentar investimento em produtos de alto desempenho",
                    "Revisar preços de produtos com baixa margem",
                    "Implementar promoções para produtos com baixa conversão",
                    "Focar em retenção de clientes repeat"
                },
                BestSellingCategory = productPerformance.FirstOrDefault()?.ProductName ?? "N/A",
                WorstPerformingProduct = worstProduct?.ProductName ?? "N/A",
                OptimalPriceRecommendation = "Considere ajustar preços com base em análise de demanda",
                GeneratedAt = DateTime.UtcNow
            };

            return insights;
        }

        public async Task<RevenueForecastDto> GetRevenueForecastAsync(Guid sellerId, int daysAhead = 30)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            if (seller.Subscription?.Plan != SellerPlan.Premium)
                throw new UnauthorizedAccessException("Apenas vendedores Premium podem acessar previsões.");

            var trends = await GetTrendsAsync(sellerId, 90);
            var avgDailyRevenue = trends.Average(t => t.Revenue);

            var forecast = new List<ForecastPointDto>();
            var now = DateTime.UtcNow;

            for (int i = 1; i <= daysAhead; i++)
            {
                var forecastDate = now.AddDays(i);
                var variation = (decimal)(new Random().NextDouble() * 0.2 - 0.1);
                var predictedRevenue = avgDailyRevenue * (1 + variation);

                forecast.Add(new ForecastPointDto
                {
                    Date = forecastDate,
                    PredictedRevenue = predictedRevenue,
                    ConfidenceInterval = 0.85m
                });
            }

            return new RevenueForecastDto
            {
                Forecast = forecast,
                ExpectedTotalRevenue = forecast.Sum(f => f.PredictedRevenue),
                Confidence = 0.85m,
                ForecastPeriodStart = now.AddDays(1),
                ForecastPeriodEnd = now.AddDays(daysAhead)
            };
        }

        public async Task<CustomerSegmentationDto> GetCustomerSegmentationAsync(Guid sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            if (seller.Subscription?.Plan != SellerPlan.Premium)
                throw new UnauthorizedAccessException("Apenas vendedores Premium podem acessar segmentação.");

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => i.SellerId == sellerId))
                .ToListAsync();

            var customerGroups = orders
                .GroupBy(o => o.BuyerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.SelectMany(o => o.Items).Where(i => i.SellerId == sellerId).Sum(i => i.Subtotal),
                    LastOrderDate = g.Max(o => o.CreatedAt)
                })
                .ToList();

            var avgSpent = customerGroups.Average(c => c.TotalSpent);
            var highValue = customerGroups.Where(c => c.TotalSpent > avgSpent * 1.5m).ToList();
            var atRisk = customerGroups.Where(c => c.LastOrderDate < DateTime.UtcNow.AddDays(-60) && c.OrderCount > 1).ToList();
            var churned = customerGroups.Where(c => c.LastOrderDate < DateTime.UtcNow.AddDays(-120)).ToList();

            return new CustomerSegmentationDto
            {
                Segments = new List<SegmentDto>
                {
                    new SegmentDto { Name = "Alto Valor", CustomerCount = highValue.Count, AverageLifetimeValue = (decimal)highValue.Average(c => c.TotalSpent), ChurnRate = 5, PurchaseFrequency = (decimal)highValue.Average(c => c.OrderCount) },
                    new SegmentDto { Name = "Em Risco", CustomerCount = atRisk.Count, AverageLifetimeValue = (decimal)atRisk.Average(c => c.TotalSpent), ChurnRate = 30, PurchaseFrequency = (decimal)atRisk.Average(c => c.OrderCount) }
                },
                HighValueSegment = new SegmentDto
                {
                    Name = "Alto Valor",
                    CustomerCount = highValue.Count,
                    AverageLifetimeValue = highValue.Count > 0 ? (decimal)highValue.Average(c => c.TotalSpent) : 0,
                    ChurnRate = 5,
                    PurchaseFrequency = highValue.Count > 0 ? (decimal)highValue.Average(c => c.OrderCount) : 0
                },
                AtRiskSegment = new SegmentDto
                {
                    Name = "Em Risco",
                    CustomerCount = atRisk.Count,
                    AverageLifetimeValue = atRisk.Count > 0 ? (decimal)atRisk.Average(c => c.TotalSpent) : 0,
                    ChurnRate = 30,
                    PurchaseFrequency = atRisk.Count > 0 ? (decimal)atRisk.Average(c => c.OrderCount) : 0
                },
                ChurnedSegment = new SegmentDto
                {
                    Name = "Perdido",
                    CustomerCount = churned.Count,
                    AverageLifetimeValue = churned.Count > 0 ? (decimal)churned.Average(c => c.TotalSpent) : 0,
                    ChurnRate = 100,
                    PurchaseFrequency = churned.Count > 0 ? (decimal)churned.Average(c => c.OrderCount) : 0
                }
            };
        }

        public async Task<SeasonalAnalysisDto> GetSeasonalAnalysisAsync(Guid sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            if (seller.Subscription?.Plan != SellerPlan.Premium)
                throw new UnauthorizedAccessException("Apenas vendedores Premium podem acessar análise sazonal.");

            var twoYearsAgo = DateTime.UtcNow.AddMonths(-24);

            var orders = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .Include(o => o.Items)
                .Where(o => o.Items.Any(i => i.SellerId == sellerId) && o.CreatedAt >= twoYearsAgo)
                .ToListAsync();

            var monthlyData = new List<MonthlyTrendDto>();
            var months = new[] { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" };

            for (int month = 1; month <= 12; month++)
            {
                var monthOrders = orders.Where(o => o.CreatedAt.Month == month).ToList();
                var avgRevenue = monthOrders.SelectMany(o => o.Items).Where(i => i.SellerId == sellerId).Sum(i => i.Subtotal) / 2;

                monthlyData.Add(new MonthlyTrendDto
                {
                    Month = months[month - 1],
                    AverageRevenue = avgRevenue,
                    AverageOrders = monthOrders.Count / 2,
                    YearOverYearGrowth = 0
                });
            }

            var peakMonth = monthlyData.OrderByDescending(m => m.AverageRevenue).First();
            var offMonth = monthlyData.OrderBy(m => m.AverageRevenue).First();

            return new SeasonalAnalysisDto
            {
                MonthlyData = monthlyData,
                PeakSeason = peakMonth.Month,
                OffSeason = offMonth.Month,
                SeasonalVariance = 0.25m,
                RecommendedStrategies = new List<string>
                {
                    "Aumentar estoque na época de pico",
                    "Promoções agressivas na época baixa",
                    "Planejar campanhas com 3 meses de antecedência"
                }
            };
        }

        public async Task<byte[]> ExportAnalyticsAsCSVAsync(Guid sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            if (seller.Subscription?.Plan != SellerPlan.Premium)
                throw new UnauthorizedAccessException("Apenas vendedores Premium podem exportar relatórios.");

            var analytics = await GetAdvancedAnalyticsAsync(sellerId);
            var productPerformance = await GetProductPerformanceAsync(sellerId);

            var csv = "Produto,Vendas,Receita,Taxa Conversão,Margem\n";
            foreach (var product in productPerformance.Take(50))
            {
                csv += $"\"{product.ProductName}\",{product.SalesCount},\"{product.Revenue:F2}\",\"{product.ConversionRate:F2}\",\"{product.Margin:F2}\"\n";
            }

            return System.Text.Encoding.UTF8.GetBytes(csv);
        }

        public async Task<byte[]> ExportAnalyticsAsPDFAsync(Guid sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.Id == sellerId && !s.IsDeleted);

            if (seller == null)
                throw new KeyNotFoundException("Vendedor não encontrado.");

            if (seller.Subscription?.Plan != SellerPlan.Premium)
                throw new UnauthorizedAccessException("Apenas vendedores Premium podem exportar relatórios.");

            var analytics = await GetAdvancedAnalyticsAsync(sellerId);

            var pdf = System.Text.Encoding.UTF8.GetBytes(
                $"Relatório de Analytics\n" +
                $"Gerado em: {DateTime.UtcNow:dd/MM/yyyy HH:mm}\n\n" +
                $"Receita Total: R$ {analytics.TotalRevenue:F2}\n" +
                $"Total de Pedidos: {analytics.TotalOrders}\n" +
                $"Clientes: {analytics.TotalCustomers}\n" +
                $"Valor Médio de Pedido: R$ {analytics.AverageOrderValue:F2}\n"
            );

            return pdf;
        }

        private decimal CalculateConversionRate(int customers, int views)
        {
            return views > 0 ? (customers / (decimal)views) * 100 : 0;
        }
    }
}




