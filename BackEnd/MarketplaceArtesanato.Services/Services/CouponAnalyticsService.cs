using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class CouponAnalyticsService : ICouponAnalyticsService
    {
        private readonly ArtesianDbContext _context;

        public CouponAnalyticsService(ArtesianDbContext context)
        {
            _context = context;
        }

        public async Task<CouponROIDto> CalculateROIAsync(Guid couponId)
        {
            var coupon = await _context.Coupons
                .Include(c => c.Usages)
                .FirstOrDefaultAsync(c => c.Id == couponId);

            if (coupon == null)
                throw new InvalidOperationException($"Cupom {couponId} não encontrado.");

            var usages = coupon.Usages?.ToList() ?? new List<CouponUsage>();
            
            var totalDiscountGiven = usages.Sum(u => u.DiscountApplied);
            var usageCount = usages.Count;

            var roi = totalDiscountGiven > 0 && usageCount > 0
                ? (totalDiscountGiven / usageCount)
                : 0;

            var conversionRate = coupon.UsageLimit > 0 
                ? ((decimal)usageCount / coupon.UsageLimit) * 100 
                : 0;

            var averageOrderValue = usageCount > 0 
                ? totalDiscountGiven / usageCount 
                : 0;

            return new CouponROIDto
            {
                CouponId = couponId,
                CouponCode = coupon.Code,
                TotalDiscountGiven = totalDiscountGiven,
                EstimatedRevenueGenerated = totalDiscountGiven * 10, // Estimativa simplificada
                ROI = roi,
                TotalUsages = usageCount,
                AverageOrderValue = averageOrderValue,
                ConversionRate = conversionRate,
                CalculatedAt = DateTime.UtcNow
            };
        }

        public async Task<SellerCouponStatsDto> GetSellerCouponStatsAsync(Guid sellerId)
        {
            var coupons = await _context.Coupons
                .Where(c => c.CreatorSellerId == sellerId && !c.IsDeleted)
                .Include(c => c.Usages)
                .ToListAsync();

            if (!coupons.Any())
            {
                return new SellerCouponStatsDto
                {
                    SellerId = sellerId,
                    ActiveCoupons = 0,
                    TotalCoupons = 0,
                    TotalDiscountSpent = 0,
                    TotalRevenueGenerated = 0,
                    AverageROI = 0,
                    ConversionRate = 0,
                    TotalCouponUsages = 0
                };
            }

            var activeCoupons = coupons.Count(c => c.IsActive && c.ValidUntil > DateTime.UtcNow);
            var totalDiscountSpent = 0m;
            var topCoupons = new List<CouponQuickStatsDto>();

            foreach (var coupon in coupons.OrderByDescending(c => c.Usages?.Count ?? 0).Take(5))
            {
                var usages = coupon.Usages?.ToList() ?? new List<CouponUsage>();
                var discountGiven = usages.Sum(u => u.DiscountApplied);
                var roi = usages.Count > 0 
                    ? (discountGiven / usages.Count)
                    : 0;

                totalDiscountSpent += discountGiven;

                topCoupons.Add(new CouponQuickStatsDto
                {
                    CouponId = coupon.Id,
                    Code = coupon.Code,
                    DiscountValue = coupon.DiscountValue,
                    Usages = usages.Count,
                    ROI = roi,
                    IsActive = coupon.IsActive && coupon.ValidUntil > DateTime.UtcNow
                });
            }

            var totalUsages = coupons.Sum(c => c.Usages?.Count ?? 0);
            var averageROI = coupons.Count > 0 
                ? coupons.Average(c => 
                {
                    var usages = c.Usages?.ToList() ?? new List<CouponUsage>();
                    var discount = usages.Sum(u => u.DiscountApplied);
                    return usages.Count > 0 ? (discount / usages.Count) : 0;
                })
                : 0;

            var conversionRate = totalUsages > 0 && coupons.Count > 0
                ? (totalUsages / (decimal)coupons.Count) * 100
                : 0;

            return new SellerCouponStatsDto
            {
                SellerId = sellerId,
                ActiveCoupons = activeCoupons,
                TotalCoupons = coupons.Count,
                TotalDiscountSpent = totalDiscountSpent,
                TotalRevenueGenerated = totalDiscountSpent * 10,
                AverageROI = averageROI,
                ConversionRate = conversionRate,
                TotalCouponUsages = totalUsages,
                TopCoupons = topCoupons
            };
        }

        public async Task<CouponPerformanceDto> GetCouponPerformanceAsync(Guid couponId, DateTime startDate, DateTime endDate)
        {
            var coupon = await _context.Coupons
                .Include(c => c.Usages)
                .FirstOrDefaultAsync(c => c.Id == couponId);

            if (coupon == null)
                throw new InvalidOperationException($"Cupom {couponId} não encontrado.");

            var usagesInPeriod = coupon.Usages?
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .ToList() ?? new List<CouponUsage>();

            var totalImpressions = coupon.Usages?.Count ?? 0;
            var totalDiscountAmount = usagesInPeriod.Sum(u => u.DiscountApplied);
            var usageCount = usagesInPeriod.Count;

            var conversionRate = totalImpressions > 0 
                ? ((decimal)usageCount / totalImpressions) * 100 
                : 0;

            var roi = usageCount > 0 
                ? (totalDiscountAmount / usageCount) 
                : 0;

            var avgOrderValue = usageCount > 0 
                ? (totalDiscountAmount * 10) / usageCount 
                : 0;

            // Agrupar dados diários
            var dailyData = usagesInPeriod
                .GroupBy(u => u.CreatedAt.Date)
                .Select(g => new DailyPerformanceDto
                {
                    Date = g.Key,
                    Impressions = g.Count(),
                    Usages = g.Count(),
                    DiscountAmount = g.Sum(u => u.DiscountApplied),
                    OrderValue = g.Sum(u => u.DiscountApplied) * 10
                })
                .OrderBy(d => d.Date)
                .ToList();

            return new CouponPerformanceDto
            {
                CouponId = couponId,
                Code = coupon.Code,
                StartDate = startDate,
                EndDate = endDate,
                TotalImpressions = totalImpressions,
                TotalUsages = usageCount,
                ConversionRate = conversionRate,
                TotalDiscountAmount = totalDiscountAmount,
                TotalOrderValue = totalDiscountAmount * 10,
                ROI = roi,
                AverageOrderValue = avgOrderValue,
                DailyData = dailyData
            };
        }

        public async Task<List<CouponPerformanceComparisonDto>> GetSellerCouponsComparisonAsync(Guid sellerId, int topN = 10)
        {
            var coupons = await _context.Coupons
                .Where(c => c.CreatorSellerId == sellerId && !c.IsDeleted)
                .Include(c => c.Usages)
                .ToListAsync();

            var comparisons = coupons
                .Select((coupon, index) =>
                {
                    var usages = coupon.Usages?.ToList() ?? new List<CouponUsage>();
                    var discountGiven = usages.Sum(u => u.DiscountApplied);
                    var roi = usages.Count > 0 
                        ? (discountGiven / usages.Count) 
                        : 0;

                    var conversionRate = coupon.UsageLimit > 0 
                        ? ((decimal)usages.Count / coupon.UsageLimit) * 100 
                        : 0;

                    var avgOrderValue = usages.Count > 0 
                        ? (discountGiven * 10) / usages.Count 
                        : 0;

                    return new CouponPerformanceComparisonDto
                    {
                        CouponId = coupon.Id,
                        Code = coupon.Code,
                        Usages = usages.Count,
                        ROI = roi,
                        ConversionRate = conversionRate,
                        AvgOrderValue = avgOrderValue,
                        Rank = index + 1
                    };
                })
                .OrderByDescending(c => c.ROI)
                .Select((c, idx) => { c.Rank = idx + 1; return c; })
                .Take(topN)
                .ToList();

            return comparisons;
        }

        public async Task<CouponAnalyticsDashboardDto> GetCouponAnalyticsDashboardAsync(Guid sellerId)
        {
            var coupons = await _context.Coupons
                .Where(c => c.CreatorSellerId == sellerId && !c.IsDeleted && c.IsActive)
                .Include(c => c.Usages)
                .ToListAsync();

            var allUsages = coupons.SelectMany(c => c.Usages ?? new List<CouponUsage>()).ToList();
            var totalSaved = allUsages.Sum(u => u.DiscountApplied);
            var activeCouponsCount = coupons.Count(c => c.ValidUntil > DateTime.UtcNow);

            var performance = new List<CouponPerformanceComparisonDto>();
            foreach (var coupon in coupons.OrderByDescending(c => c.Usages?.Count ?? 0))
            {
                var usages = coupon.Usages?.ToList() ?? new List<CouponUsage>();
                var discount = usages.Sum(u => u.DiscountApplied);
                var roi = usages.Count > 0 ? (discount / usages.Count) : 0;
                var conversionRate = coupon.UsageLimit > 0 
                    ? ((decimal)usages.Count / coupon.UsageLimit) * 100 
                    : 0;
                var avgOrderValue = usages.Count > 0 ? (discount * 10) / usages.Count : 0;

                performance.Add(new CouponPerformanceComparisonDto
                {
                    CouponId = coupon.Id,
                    Code = coupon.Code,
                    Usages = usages.Count,
                    ROI = roi,
                    ConversionRate = conversionRate,
                    AvgOrderValue = avgOrderValue
                });
            }

            var averageROI = performance.Any() ? performance.Average(p => p.ROI) : 0;
            var avgConversionRate = performance.Any() ? performance.Average(p => p.ConversionRate) : 0;

            var topPerformers = performance.OrderByDescending(p => p.ROI).Take(5).ToList();
            var bottomPerformers = performance.OrderBy(p => p.ROI).Take(5).ToList();

            // Calcular trend mensal
            var thisMonth = allUsages.Where(u => u.CreatedAt.Month == DateTime.UtcNow.Month).Sum(u => u.DiscountApplied);
            var lastMonth = allUsages.Where(u => u.CreatedAt.Month == DateTime.UtcNow.AddMonths(-1).Month).Sum(u => u.DiscountApplied);
            var monthlyTrend = lastMonth > 0 ? ((thisMonth - lastMonth) / lastMonth) * 100 : 0;

            return new CouponAnalyticsDashboardDto
            {
                TotalSavedByCustomers = totalSaved,
                ActiveCouponsCount = activeCouponsCount,
                AverageROI = averageROI,
                ConversionRate = avgConversionRate,
                TopPerformers = topPerformers,
                BottomPerformers = bottomPerformers,
                MonthlyTrend = monthlyTrend
            };
        }
    }
}

