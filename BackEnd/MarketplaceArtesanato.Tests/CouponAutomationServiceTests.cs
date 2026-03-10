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
    public class CouponAutomationServiceTests
    {
        private ArtesianDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ArtesianDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ArtesianDbContext(options);
        }

        [Fact]
        public async Task DeactivateExpiredCoupons_WithExpiredCoupons_DeactivatesThem()
        {
            // Arrange
            var context = GetInMemoryContext();
            var now = DateTime.UtcNow;

            var expiredCoupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "EXPIRED",
                Type = CouponType.Platform,
                DiscountType = DiscountType.Fixed,
                DiscountValue = 10,
                IsActive = true,
                ValidFrom = now.AddDays(-10),
                ValidUntil = now.AddDays(-1), // Expirado ontem
                UsageLimit = 100
            };

            var validCoupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "VALID",
                Type = CouponType.Platform,
                DiscountType = DiscountType.Fixed,
                DiscountValue = 5,
                IsActive = true,
                ValidFrom = now.AddDays(-1),
                ValidUntil = now.AddDays(10), // Válido por mais 10 dias
                UsageLimit = 50
            };

            context.Coupons.AddRange(expiredCoupon, validCoupon);
            await context.SaveChangesAsync();

            var service = new CouponAutomationService(context);

            // Act
            await service.DeactivateExpiredCouponsAsync();

            // Assert
            var deactivatedCoupon = await context.Coupons.FindAsync(expiredCoupon.Id);
            var stillActiveCoupon = await context.Coupons.FindAsync(validCoupon.Id);

            Assert.False(deactivatedCoupon.IsActive);
            Assert.True(stillActiveCoupon.IsActive);
        }

        [Fact]
        public async Task ApplyAutomaticLimits_WithReachedLimit_DeactivatesCoupon()
        {
            // Arrange
            var context = GetInMemoryContext();
            var couponId = Guid.NewGuid();

            var coupon = new Coupon
            {
                Id = couponId,
                Code = "LIMITED",
                Type = CouponType.Seller,
                DiscountType = DiscountType.Fixed,
                DiscountValue = 10,
                IsActive = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidUntil = DateTime.UtcNow.AddDays(30),
                UsageLimit = 3, // Limite de 3 usos
                Usages = new List<CouponUsage>
                {
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = couponId, DiscountApplied = 10, CreatedAt = DateTime.UtcNow },
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = couponId, DiscountApplied = 10, CreatedAt = DateTime.UtcNow },
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = couponId, DiscountApplied = 10, CreatedAt = DateTime.UtcNow }
                }
            };

            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();

            var service = new CouponAutomationService(context);

            // Act
            await service.ApplyAutomaticLimitsAsync();

            // Assert
            var updatedCoupon = await context.Coupons.FindAsync(couponId);
            Assert.False(updatedCoupon.IsActive);
        }

        [Fact]
        public async Task ApplyAutomaticLimits_WithUnreachedLimit_RemainActive()
        {
            // Arrange
            var context = GetInMemoryContext();
            var couponId = Guid.NewGuid();

            var coupon = new Coupon
            {
                Id = couponId,
                Code = "NOTLIMITED",
                Type = CouponType.Seller,
                DiscountType = DiscountType.Fixed,
                DiscountValue = 5,
                IsActive = true,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidUntil = DateTime.UtcNow.AddDays(30),
                UsageLimit = 100, // Limite alto
                Usages = new List<CouponUsage>
                {
                    new CouponUsage { Id = Guid.NewGuid(), CouponId = couponId, DiscountApplied = 5, CreatedAt = DateTime.UtcNow }
                }
            };

            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();

            var service = new CouponAutomationService(context);

            // Act
            await service.ApplyAutomaticLimitsAsync();

            // Assert
            var updatedCoupon = await context.Coupons.FindAsync(couponId);
            Assert.True(updatedCoupon.IsActive);
        }

        [Fact]
        public async Task ApplySeasonalCoupons_ActivatesCouponsInValidDateRange()
        {
            // Arrange
            var context = GetInMemoryContext();
            var now = DateTime.UtcNow;
            var couponId = Guid.NewGuid();

            var seasonalCoupon = new Coupon
            {
                Id = couponId,
                Code = "SEASONAL",
                Type = CouponType.Intelligent,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 15,
                IsActive = false, // Inicialmente inativo
                ValidFrom = now.AddSeconds(-1), // Começa agora
                ValidUntil = now.AddDays(7), // Válido por 7 dias
                UsageLimit = 1000
            };

            context.Coupons.Add(seasonalCoupon);
            await context.SaveChangesAsync();

            var service = new CouponAutomationService(context);

            // Act
            await service.ApplySeasonalCouponsAsync();

            // Assert
            var updatedCoupon = await context.Coupons.FindAsync(couponId);
            Assert.True(updatedCoupon.IsActive);
        }

        [Fact]
        public async Task ApplySeasonalCoupons_DeactivatesCouponsAfterValidUntil()
        {
            // Arrange
            var context = GetInMemoryContext();
            var now = DateTime.UtcNow;
            var couponId = Guid.NewGuid();

            var expiredSeasonalCoupon = new Coupon
            {
                Id = couponId,
                Code = "EXPIRED_SEASONAL",
                Type = CouponType.Intelligent,
                DiscountType = DiscountType.Percentage,
                DiscountValue = 20,
                IsActive = true, // Ativo
                ValidFrom = now.AddDays(-10),
                ValidUntil = now.AddSeconds(-1), // Expirou há alguns segundos
                UsageLimit = 500
            };

            context.Coupons.Add(expiredSeasonalCoupon);
            await context.SaveChangesAsync();

            var service = new CouponAutomationService(context);

            // Act
            await service.ApplySeasonalCouponsAsync();

            // Assert
            var updatedCoupon = await context.Coupons.FindAsync(couponId);
            Assert.False(updatedCoupon.IsActive);
        }

        [Fact]
        public async Task ExecuteAllAutomations_RunsAllAutomationsSequentially()
        {
            // Arrange
            var context = GetInMemoryContext();
            var now = DateTime.UtcNow;

            var expiredCoupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "EXPIRED",
                Type = CouponType.Platform,
                DiscountType = DiscountType.Fixed,
                DiscountValue = 10,
                IsActive = true,
                ValidFrom = now.AddDays(-5),
                ValidUntil = now.AddDays(-1),
                UsageLimit = 100
            };

            var limitedCoupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = "LIMITED",
                Type = CouponType.Seller,
                DiscountType = DiscountType.Fixed,
                DiscountValue = 5,
                IsActive = true,
                ValidFrom = now.AddDays(-1),
                ValidUntil = now.AddDays(30),
                UsageLimit = 1,
                Usages = new List<CouponUsage>
                {
                    new CouponUsage { Id = Guid.NewGuid(), DiscountApplied = 5, CreatedAt = now }
                }
            };

            context.Coupons.AddRange(expiredCoupon, limitedCoupon);
            await context.SaveChangesAsync();

            var service = new CouponAutomationService(context);

            // Act
            await service.ExecuteAllAutomationsAsync();

            // Assert
            var updatedExpired = await context.Coupons.FindAsync(expiredCoupon.Id);
            var updatedLimited = await context.Coupons.FindAsync(limitedCoupon.Id);

            Assert.False(updatedExpired.IsActive); // Desativado por expiração
            Assert.False(updatedLimited.IsActive); // Desativado por limite atingido
        }

        [Fact]
        public async Task GetAutomationLogs_ReturnsRecentLogs()
        {
            // Arrange
            var context = GetInMemoryContext();
            var contextService = new CouponAutomationService(context);

            // Act - Executar automações para gerar logs
            await contextService.ExecuteAllAutomationsAsync();
            var logs = await contextService.GetAutomationLogsAsync(days: 7);

            // Assert
            Assert.NotNull(logs);
            Assert.NotEmpty(logs);
            Assert.True(logs.Count >= 3); // Deve ter pelo menos 3 automações (Expiration, Limit, Seasonal)
        }
    }
}
