using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Services.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MarketplaceArtesanato.Tests
{
    public class SellerAnalyticsExportServiceTests
    {
        private ArtesianDbContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ArtesianDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ArtesianDbContext(options);
        }

        private ISellerAnalyticsService GetAnalyticsService(ArtesianDbContext context)
        {
            return new SellerAnalyticsService(context);
        }

        private void SeedTestDataWithPlan(ArtesianDbContext context, Guid sellerId, SellerPlan plan)
        {
            var subscription = new SellerSubscription
            {
                Id = Guid.NewGuid(),
                SellerId = sellerId,
                Plan = plan,
                IsActive = true,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var seller = new Seller
            {
                Id = sellerId,
                StoreName = "Test Store",
                Subscription = subscription,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var buyerId = Guid.NewGuid();
            var buyer = new User
            {
                Id = buyerId,
                Name = "Test Buyer",
                Email = "buyer@test.com",
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
                CreatedAt = DateTime.UtcNow
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
                CreatedAt = DateTime.UtcNow
            };

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
                CreatedAt = DateTime.UtcNow,
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
                        CreatedAt = DateTime.UtcNow
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
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            context.SellerSubscriptions.Add(subscription);
            context.Sellers.Add(seller);
            context.Users.Add(buyer);
            context.Products.AddRange(product1, product2);
            context.Orders.Add(order1);
            context.SaveChanges();
        }

        [Fact]
        public async Task ExportAnalyticsAsCSVAsync_WithPremiumSeller_ReturnsValidCSV()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestDataWithPlan(context, sellerId, SellerPlan.Premium);
            var service = GetAnalyticsService(context);

            // Act
            var csvBytes = await service.ExportAnalyticsAsCSVAsync(sellerId);

            // Assert
            Assert.NotNull(csvBytes);
            Assert.True(csvBytes.Length > 0);

            var csvContent = Encoding.UTF8.GetString(csvBytes);
            Assert.Contains("Relatório de Analytics", csvContent);
            Assert.Contains("Métricas Gerais", csvContent);
            Assert.Contains("Desempenho de Produtos", csvContent);
            Assert.Contains("Receita Total", csvContent);
            Assert.Contains("R$", csvContent);
        }

        [Fact]
        public async Task ExportAnalyticsAsCSVAsync_WithBasicSeller_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestDataWithPlan(context, sellerId, SellerPlan.Basic);
            var service = GetAnalyticsService(context);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.ExportAnalyticsAsCSVAsync(sellerId)
            );
        }

        [Fact]
        public async Task ExportAnalyticsAsCSVAsync_WithInvalidSeller_ThrowsKeyNotFoundException()
        {
            // Arrange
            var context = GetInMemoryContext();
            var invalidSellerId = Guid.NewGuid();
            var service = GetAnalyticsService(context);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ExportAnalyticsAsCSVAsync(invalidSellerId)
            );
        }

        [Fact]
        public async Task ExportAnalyticsAsCSVAsync_ContainsProperCSVFormat()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestDataWithPlan(context, sellerId, SellerPlan.Premium);
            var service = GetAnalyticsService(context);

            // Act
            var csvBytes = await service.ExportAnalyticsAsCSVAsync(sellerId);
            var csvContent = Encoding.UTF8.GetString(csvBytes);

            // Assert - Check for CSV structure
            var lines = csvContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            Assert.True(lines.Length > 5, "CSV should have multiple lines");
            Assert.Contains("Produto", csvContent);
            Assert.Contains("Posição", csvContent);
            Assert.Contains("Vendas", csvContent);
            Assert.Contains("Receita", csvContent);
        }

        [Fact]
        public async Task ExportAnalyticsAsPDFAsync_WithPremiumSeller_ReturnsValidPDF()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestDataWithPlan(context, sellerId, SellerPlan.Premium);
            var service = GetAnalyticsService(context);

            // Act
            var pdfBytes = await service.ExportAnalyticsAsPDFAsync(sellerId);

            // Assert
            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 0);

            // PDF files start with %PDF magic bytes
            Assert.True(pdfBytes[0] == 0x25 && pdfBytes[1] == 0x50 && pdfBytes[2] == 0x44 && pdfBytes[3] == 0x46,
                "PDF file should start with %PDF magic bytes");
        }

        [Fact]
        public async Task ExportAnalyticsAsPDFAsync_WithBasicSeller_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestDataWithPlan(context, sellerId, SellerPlan.Basic);
            var service = GetAnalyticsService(context);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.ExportAnalyticsAsPDFAsync(sellerId)
            );
        }

        [Fact]
        public async Task ExportAnalyticsAsPDFAsync_WithInvalidSeller_ThrowsKeyNotFoundException()
        {
            // Arrange
            var context = GetInMemoryContext();
            var invalidSellerId = Guid.NewGuid();
            var service = GetAnalyticsService(context);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ExportAnalyticsAsPDFAsync(invalidSellerId)
            );
        }

        [Fact]
        public async Task ExportAnalyticsAsPDFAsync_ContainsProperStructure()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestDataWithPlan(context, sellerId, SellerPlan.Premium);
            var service = GetAnalyticsService(context);

            // Act
            var pdfBytes = await service.ExportAnalyticsAsPDFAsync(sellerId);
            var pdfContent = Encoding.UTF8.GetString(pdfBytes, 0, Math.Min(1000, pdfBytes.Length));

            // Assert - Check for PDF content indicators
            Assert.Contains("/Type", pdfContent);
            Assert.Contains("/Catalog", pdfContent);
            Assert.Contains("stream", pdfContent);
        }

        [Fact]
        public async Task ExportAnalyticsAsCSVAsync_ProSeller_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestDataWithPlan(context, sellerId, SellerPlan.Pro);
            var service = GetAnalyticsService(context);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.ExportAnalyticsAsCSVAsync(sellerId)
            );
        }

        [Fact]
        public async Task ExportAnalyticsAsPDFAsync_ProSeller_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestDataWithPlan(context, sellerId, SellerPlan.Pro);
            var service = GetAnalyticsService(context);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.ExportAnalyticsAsPDFAsync(sellerId)
            );
        }

        [Fact]
        public async Task ExportAnalyticsAsCSVAsync_MultipleCallsProduceDifferentTimestamps()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestDataWithPlan(context, sellerId, SellerPlan.Premium);
            var service = GetAnalyticsService(context);

            // Act
            var csv1 = await service.ExportAnalyticsAsCSVAsync(sellerId);
            await Task.Delay(100);
            var csv2 = await service.ExportAnalyticsAsCSVAsync(sellerId);

            // Assert - Timestamps should be different
            var content1 = Encoding.UTF8.GetString(csv1);
            var content2 = Encoding.UTF8.GetString(csv2);
            Assert.NotEqual(content1, content2);
        }

        [Fact]
        public async Task ExportAnalyticsAsPDFAsync_MultipleCallsProduceDifferentTimestamps()
        {
            // Arrange
            var context = GetInMemoryContext();
            var sellerId = Guid.NewGuid();
            SeedTestDataWithPlan(context, sellerId, SellerPlan.Premium);
            var service = GetAnalyticsService(context);

            // Act
            var pdf1 = await service.ExportAnalyticsAsPDFAsync(sellerId);
            await Task.Delay(100);
            var pdf2 = await service.ExportAnalyticsAsPDFAsync(sellerId);

            // Assert - PDFs should be different (timestamps change)
            var content1 = Encoding.UTF8.GetString(pdf1);
            var content2 = Encoding.UTF8.GetString(pdf2);
            Assert.NotEqual(content1, content2);
        }
    }
}
