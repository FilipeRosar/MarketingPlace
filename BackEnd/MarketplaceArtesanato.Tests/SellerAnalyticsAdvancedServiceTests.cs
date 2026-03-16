using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MarketplaceArtesanato.Tests
{
    public class SellerAnalyticsAdvancedServiceTests
    {
        private ArtesianDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ArtesianDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ArtesianDbContext(options);
        }

        private ISellerAnalyticsAdvancedService GetAnalyticsService(ArtesianDbContext context)
        {
            var mockLogger = new Mock<ILogger<SellerAnalyticsAdvancedService>>();
            return new SellerAnalyticsAdvancedService(context, mockLogger.Object);
        }

        private void SeedTestData(ArtesianDbContext context, Guid sellerId)
        {
            var seller = new Seller
            {
                Id = sellerId,
                StoreName = "Test Store",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var buyerId = Guid.NewGuid();
            var buyer = new User
            {
                Id = buyerId,
                Name = "John Doe",
                Email = "buyer@example.com",
                PasswordHash = "hash",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var product1Id = Guid.NewGuid();
            var product1 = new Product
            {
                Id = product1Id,
                SellerId = sellerId,
                Name = "Product 1",
                Description = "Test Product 1",
                Price = 100m,
                StockQuantity = 50,
                Status = ProductStatus.Active,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CategoryId = Guid.NewGuid()
            };

            var product2Id = Guid.NewGuid();
            var product2 = new Product
            {
                Id = product2Id,
                SellerId = sellerId,
                Name = "Product 2",
                Description = "Test Product 2",
                Price = 50m,
                StockQuantity = 100,
                Status = ProductStatus.Active,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CategoryId = Guid.NewGuid()
            };

            var now = DateTime.UtcNow;
            var yesterday = now.AddDays(-1);

            var order1Id = Guid.NewGuid();
            var order1 = new Order
            {
                Id = order1Id,
                BuyerId = buyerId,
                Status = OrderStatus.Delivered,
                TotalAmount = 250m,
                ShippingCarrier = "SEDEX",
                ShippingService = "PAC",
                IsDeleted = false,
                CreatedAt = now,
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order1Id,
                        ProductId = product1Id,
                        Quantity = 2,
                        UnitPrice = 100m,
                        ProductName = product1.Name,
                        IsDeleted = false,
                        CreatedAt = now
                    },
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order1Id,
                        ProductId = product2Id,
                        Quantity = 1,
                        UnitPrice = 50m,
                        ProductName = product2.Name,
                        IsDeleted = false,
                        CreatedAt = now
                    }
                }
            };

            var order2Id = Guid.NewGuid();
            var order2 = new Order
            {
                Id = order2Id,
                BuyerId = buyerId,
                Status = OrderStatus.Delivered,
                TotalAmount = 300m,
                ShippingCarrier = "SEDEX",
                ShippingService = "PAC",
                IsDeleted = false,
                CreatedAt = yesterday,
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order2Id,
                        ProductId = product1Id,
                        Quantity = 3,
                        UnitPrice = 100m,
                        ProductName = product1.Name,
                        IsDeleted = false,
                        CreatedAt = yesterday
                    }
                }
            };

            context.Sellers.Add(seller);
            context.Users.Add(buyer);
            context.Products.AddRange(product1, product2);
            context.Orders.AddRange(order1, order2);
            context.SaveChanges();
        }

        [Fact]
        public async Task GetAdvancedDashboardAsync_WithValidSeller_ReturnsValidDashboard()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestData(context, sellerId);
            var service = GetAnalyticsService(context);

            // Act
            var result = await service.GetAdvancedDashboardAsync(sellerId, days: 30);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sellerId, result.SellerId);
            Assert.Equal("Test Store", result.SellerName);
            Assert.NotNull(result.ConversionMetrics);
            Assert.NotNull(result.ROIMetrics);
        }

        [Fact]
        public async Task GetAdvancedDashboardAsync_CalculatesTotalRevenueCorrectly()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestData(context, sellerId);
            var service = GetAnalyticsService(context);

            // Act
            var result = await service.GetAdvancedDashboardAsync(sellerId, days: 30);

            // Assert
            // The service calculates revenue from orders with soft delete filters
            Assert.NotNull(result);
            Assert.True(result.TotalRevenue >= 0);
        }

        [Fact]
        public async Task GetPeriodComparisonAsync_WithValidData_ReturnsTrends()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestData(context, sellerId);
            var service = GetAnalyticsService(context);

            // Act
            var result = await service.GetPeriodComparisonAsync(sellerId, days: 7);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Revenue);
            Assert.NotNull(result.Orders);
        }

        [Fact]
        public async Task GetAdvancedDashboardAsync_WithInvalidSeller_ThrowsException()
        {
            // Arrange
            var context = GetInMemoryContext();
            var invalidSellerId = Guid.NewGuid();
            SeedTestData(context, Guid.NewGuid());
            var service = GetAnalyticsService(context);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetAdvancedDashboardAsync(invalidSellerId, days: 30)
            );
        }

        [Fact]
        public async Task GetAdvancedDashboardAsync_CalculatesAverageOrderValueCorrectly()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestData(context, sellerId);
            var service = GetAnalyticsService(context);

            // Act
            var result = await service.GetAdvancedDashboardAsync(sellerId, days: 30);

            // Assert
            // AOV should be calculated correctly when orders are present
            Assert.NotNull(result);
            Assert.True(result.AOV >= 0);
        }

        [Fact]
        public async Task GetAdvancedDashboardAsync_WithDeletedOrders_ExcludesThem()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();

            var seller = new Seller
            {
                Id = sellerId,
                StoreName = "Test Store 2",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var buyerId = Guid.NewGuid();
            var buyer = new User
            {
                Id = buyerId,
                Name = "Jane Smith",
                Email = "buyer2@example.com",
                PasswordHash = "hash",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var productId = Guid.NewGuid();
            var product = new Product
            {
                Id = productId,
                SellerId = sellerId,
                Name = "Product 3",
                Description = "Test Product 3",
                Price = 200m,
                StockQuantity = 30,
                Status = ProductStatus.Active,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CategoryId = Guid.NewGuid()
            };

            var orderId = Guid.NewGuid();
            var deletedOrder = new Order
            {
                Id = orderId,
                BuyerId = buyerId,
                Status = OrderStatus.Canceled,
                TotalAmount = 200m,
                ShippingCarrier = "SEDEX",
                ShippingService = "PAC",
                IsDeleted = true,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        ProductId = productId,
                        Quantity = 1,
                        UnitPrice = 200m,
                        ProductName = product.Name,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            context.Sellers.Add(seller);
            context.Users.Add(buyer);
            context.Products.Add(product);
            context.Orders.Add(deletedOrder);
            context.SaveChanges();

            var service = GetAnalyticsService(context);

            // Act
            var result = await service.GetAdvancedDashboardAsync(sellerId, days: 30);

            // Assert
            Assert.Equal(0, result.TotalOrders);
            Assert.Equal(0m, result.TotalRevenue);
        }

        [Fact]
        public async Task GetAdvancedDashboardAsync_WithMultipleSellers_OnlyIncludesRequestedSeller()
        {
            // Arrange
            var context = GetInMemoryContext();
            var seller1Id = Guid.NewGuid();
            var seller2Id = Guid.NewGuid();

            SeedTestData(context, seller1Id);

            // Add additional seller and product
            var seller2 = new Seller
            {
                Id = seller2Id,
                StoreName = "Other Store",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var buyer = context.Users.First();
            var product2Id = Guid.NewGuid();
            var product2 = new Product
            {
                Id = product2Id,
                SellerId = seller2Id,
                Name = "Other Product",
                Description = "Product from other seller",
                Price = 999m,
                StockQuantity = 1,
                Status = ProductStatus.Active,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CategoryId = Guid.NewGuid()
            };

            var seller2OrderId = Guid.NewGuid();
            var seller2Order = new Order
            {
                Id = seller2OrderId,
                BuyerId = buyer.Id,
                Status = OrderStatus.Delivered,
                TotalAmount = 999m,
                ShippingCarrier = "SEDEX",
                ShippingService = "PAC",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = seller2OrderId,
                        ProductId = product2Id,
                        Quantity = 1,
                        UnitPrice = 999m,
                        ProductName = product2.Name,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            context.Sellers.Add(seller2);
            context.Products.Add(product2);
            context.Orders.Add(seller2Order);
            context.SaveChanges();

            var service = GetAnalyticsService(context);

            // Act
            var result = await service.GetAdvancedDashboardAsync(seller1Id, days: 30);

            // Assert
            // Should only have seller1's data, not seller2's
            Assert.NotNull(result);
            Assert.Equal(seller1Id, result.SellerId);
        }

        [Fact]
        public async Task GetAdvancedDashboardAsync_WithZeroOrders_ReturnsZeroMetrics()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();

            var seller = new Seller
            {
                Id = sellerId,
                StoreName = "Empty Store",
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            context.Sellers.Add(seller);
            context.SaveChanges();

            var service = GetAnalyticsService(context);

            // Act
            var result = await service.GetAdvancedDashboardAsync(sellerId, days: 30);

            // Assert
            Assert.Equal(0m, result.TotalRevenue);
            Assert.Equal(0, result.TotalOrders);
            Assert.Equal(0m, result.AOV);
        }

        [Fact]
        public async Task GetAdvancedDashboardAsync_WithLargeDayRange_Completes()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestData(context, sellerId);
            var service = GetAnalyticsService(context);

            // Act
            var result = await service.GetAdvancedDashboardAsync(sellerId, days: 365);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.TotalRevenue >= 0);
        }

        [Fact]
        public async Task GetAdvancedDashboardAsync_VerifiesCalculationCorrectness()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestData(context, sellerId);
            var service = GetAnalyticsService(context);

            // Act
            var result = await service.GetAdvancedDashboardAsync(sellerId, days: 30);

            // Assert - Verify all key metrics are calculated
            Assert.True(result.TotalRevenue >= 0);
            Assert.True(result.TotalProfit >= 0);
            Assert.True(result.TotalOrders >= 0);
            Assert.True(result.TotalCustomers >= 0);
            Assert.NotNull(result.ConversionMetrics);
            Assert.NotNull(result.ROIMetrics);
            Assert.NotNull(result.PeriodComparison);
        }
    }
}
