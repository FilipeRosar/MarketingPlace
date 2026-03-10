using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Services.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MarketplaceArtesanato.Tests
{
    public class CouponAnalyticsServiceTests
    {
        private ArtesianDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ArtesianDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ArtesianDbContext(options);
        }

        [Fact]
        public async Task CalculateROI_WithUsages_ReturnsCorrectROI()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            var couponId = Guid.NewGuid();

            var coupon = new Coupon
            {
                Id = couponId,
                Code = "TEST10",
                Description = "Test Coupon",
                Type = CouponType.Seller,
                DiscountType = DiscountType.Fixed,
                DiscountValue = 10,
                IsActive = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidUntil = DateTime.UtcNow.AddDays(1),
                CreatorSellerId = sellerId,
                UsageLimit = 100,
                Usages = new List<CouponUsage>
                {
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = couponId, DiscountApplied = 10, CreatedAt = DateTime.UtcNow },
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = couponId, DiscountApplied = 10, CreatedAt = DateTime.UtcNow },
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = couponId, DiscountApplied = 10, CreatedAt = DateTime.UtcNow }
                }
            };

            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();

            var service = new CouponAnalyticsService(context);

            // Act
            var roi = await service.CalculateROIAsync(couponId);

            // Assert
            Assert.NotNull(roi);
            Assert.Equal(couponId, roi.CouponId);
            Assert.Equal("TEST10", roi.CouponCode);
            Assert.Equal(30, roi.TotalDiscountGiven);
            Assert.Equal(3, roi.TotalUsages);
            Assert.True(roi.ROI > 0);
        }

        [Fact]
        public async Task GetSellerCouponStats_WithMultipleCoupons_ReturnsAggregatedStats()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            var coupon1Id = Guid.NewGuid();
            var coupon2Id = Guid.NewGuid();

            var coupon1 = new Coupon
            {
                Id = coupon1Id,
                Code = "SALE1",
                Type = CouponType.Seller,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10,
                IsActive = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidUntil = DateTime.UtcNow.AddDays(30),
                CreatorSellerId = sellerId,
                UsageLimit = 100,
                Usages = new List<CouponUsage>
                {
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = coupon1Id, DiscountApplied = 10, CreatedAt = DateTime.UtcNow },
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = coupon1Id, DiscountApplied = 20, CreatedAt = DateTime.UtcNow }
                }
            };

            var coupon2 = new Coupon
            {
                Id = coupon2Id,
                Code = "SALE2",
                Type = CouponType.Seller,
                DiscountType = DiscountType.Fixed,
                DiscountValue = 5,
                IsActive = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidUntil = DateTime.UtcNow.AddDays(7),
                CreatorSellerId = sellerId,
                UsageLimit = 50,
                Usages = new List<CouponUsage>
                {
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = coupon2Id, DiscountApplied = 5, CreatedAt = DateTime.UtcNow }
                }
            };

            context.Coupons.AddRange(coupon1, coupon2);
            await context.SaveChangesAsync();

            var service = new CouponAnalyticsService(context);

            // Act
            var stats = await service.GetSellerCouponStatsAsync(sellerId);

            // Assert
            Assert.NotNull(stats);
            Assert.Equal(sellerId, stats.SellerId);
            Assert.Equal(2, stats.TotalCoupons);
            Assert.Equal(2, stats.ActiveCoupons);
            Assert.Equal(35, stats.TotalDiscountSpent); // 10+20+5
            Assert.Equal(3, stats.TotalCouponUsages);
            Assert.True(stats.TopCoupons.Count <= 5);
        }

        [Fact]
        public async Task GetCouponPerformance_WithDateRange_ReturnsFilteredData()
        {
            // Arrange
            var context = GetInMemoryContext();
            var couponId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;

            var coupon = new Coupon
            {
                Id = couponId,
                Code = "PERF",
                Type = CouponType.Seller,
                DiscountType = DiscountType.Fixed,
                DiscountValue = 5,
                IsActive = true,
                ValidFrom = today.AddDays(-30),
                ValidUntil = today.AddDays(30),
                UsageLimit = 100,
                Usages = new List<CouponUsage>
                {
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = couponId, DiscountApplied = 5, CreatedAt = today.AddDays(-5) },
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = couponId, DiscountApplied = 5, CreatedAt = today },
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = couponId, DiscountApplied = 5, CreatedAt = today }
                }
            };

            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();

            var service = new CouponAnalyticsService(context);

            // Act
            var performance = await service.GetCouponPerformanceAsync(couponId, today, today.AddDays(1));

            // Assert
            Assert.NotNull(performance);
            Assert.Equal(couponId, performance.CouponId);
            Assert.Equal(2, performance.TotalUsages); // Apenas os usos de hoje
            Assert.Equal(10, performance.TotalDiscountAmount);
        }

        [Fact]
        public async Task CalculateROI_WithoutUsages_ReturnsZeroROI()
        {
            // Arrange
            var context = GetInMemoryContext();
            var couponId = Guid.NewGuid();

            var coupon = new Coupon
            {
                Id = couponId,
                Code = "EMPTY",
                Type = CouponType.Seller,
                DiscountType = DiscountType.Fixed,
                DiscountValue = 10,
                IsActive = true,
                ValidFrom = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddDays(7),
                UsageLimit = 100,
                Usages = new List<CouponUsage>()
            };

            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();

            var service = new CouponAnalyticsService(context);

            // Act
            var roi = await service.CalculateROIAsync(couponId);

            // Assert
            Assert.NotNull(roi);
            Assert.Equal(0, roi.TotalUsages);
            Assert.Equal(0, roi.ROI);
            Assert.Equal(0, roi.AverageOrderValue);
        }
    }
}
