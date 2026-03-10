using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Services.Services;
using Moq;
using Xunit;

namespace MarketplaceArtesanato.Tests;

public class PaymentIntegrationTests
{
    private readonly Mock<ISellerSubscriptionService> _mockSubscriptionService;
    private readonly Mock<IPlatformFeeService> _mockPlatformFeeService;
    private readonly CommissionCalculationService _commissionService;

    public PaymentIntegrationTests()
    {
        _mockSubscriptionService = new Mock<ISellerSubscriptionService>();
        _mockPlatformFeeService = new Mock<IPlatformFeeService>();
        _commissionService = new CommissionCalculationService(
            _mockSubscriptionService.Object,
            _mockPlatformFeeService.Object);
    }

    [Fact]
    public async Task PaymentSplit_ShouldMatchCheckoutCalculation()
    {
        // Arrange - Simular carrinho com dois vendedores
        var seller1 = new Seller { Id = Guid.NewGuid(), StoreName = "Store 1", CommissionRate = 12 };
        var seller2 = new Seller { Id = Guid.NewGuid(), StoreName = "Store 2", CommissionRate = 9 };

        _mockPlatformFeeService
            .Setup(x => x.GetCommissionRateAsync(seller1.Id, It.IsAny<decimal>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(12);

        _mockPlatformFeeService
            .Setup(x => x.GetCommissionRateAsync(seller2.Id, It.IsAny<decimal>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(9);

        decimal seller1Gross = 1000m;
        decimal seller2Gross = 2000m;
        decimal totalProductAmount = seller1Gross + seller2Gross;

        // Act - Calcular durante checkout
        var (commission1, serviceFee1, _) = 
            await _commissionService.CalculateFeesAsync(seller1Gross, seller1);
        var (commission2, serviceFee2, _) = 
            await _commissionService.CalculateFeesAsync(seller2Gross, seller2);

        decimal totalServiceFee = serviceFee1 + serviceFee2;
        decimal totalCommission = commission1 + commission2;

        // Act - Simular webhook processing
        var (webhookCommission1, _, _) = 
            await _commissionService.CalculateFeesAsync(seller1Gross, seller1);
        var (webhookCommission2, _, _) = 
            await _commissionService.CalculateFeesAsync(seller2Gross, seller2);

        // Assert - Valores devem bater exatamente
        Assert.Equal(commission1, webhookCommission1);
        Assert.Equal(commission2, webhookCommission2);
        Assert.Equal(120m, commission1); // 1000 * 0.12
        Assert.Equal(180m, commission2); // 2000 * 0.09
        Assert.Equal(75m, totalServiceFee); // (1000 + 2000) * 0.025
        Assert.Equal(300m, totalCommission); // 120 + 180
    }

    [Fact]
    public async Task ServiceFeePercentage_ShouldBeConsistent()
    {
        // Arrange
        var orders = new List<decimal> { 100m, 500m, 1000m, 5000m, 10000m };
        decimal expectedPercentage = 0.025m; // 2.5%

        // Act & Assert
        foreach (var orderAmount in orders)
        {
            var serviceFee = await _commissionService.GetServiceFeePercentageAsync();
            var expectedFee = orderAmount * expectedPercentage;

            // Verificar que a taxa percentual é consistente
            Assert.Equal(expectedPercentage, serviceFee);
        }
    }

    [Fact]
    public async Task MultipleOrders_ShouldCalculateConsistently()
    {
        // Arrange
        var seller = new Seller { Id = Guid.NewGuid(), StoreName = "Test Store", CommissionRate = 12 };
        _mockPlatformFeeService
            .Setup(x => x.GetCommissionRateAsync(seller.Id, It.IsAny<decimal>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(12);

        var orders = new List<decimal> { 1000m, 2000m, 5000m, 10000m };
        var results = new List<(decimal Commission, decimal ServiceFee)>();

        // Act
        foreach (var orderAmount in orders)
        {
            var (commission, serviceFee, _) = 
                await _commissionService.CalculateFeesAsync(orderAmount, seller);
            results.Add((commission, serviceFee));
        }

        // Assert - Verificar consistência de proporções
        for (int i = 0; i < results.Count; i++)
        {
            var expectedCommission = orders[i] * 0.12m;
            var expectedServiceFee = orders[i] * 0.025m;

            Assert.Equal(expectedCommission, results[i].Commission);
            Assert.Equal(expectedServiceFee, results[i].ServiceFee);
        }
    }

    [Fact]
    public async Task NegativeOrderAmount_ShouldFail()
    {
        // Arrange
        var seller = new Seller { Id = Guid.NewGuid(), StoreName = "Test Store" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _commissionService.CalculateFeesAsync(-1000m, seller));
    }

    [Fact]
    public async Task ZeroOrderAmount_ShouldFail()
    {
        // Arrange
        var seller = new Seller { Id = Guid.NewGuid(), StoreName = "Test Store" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _commissionService.CalculateFeesAsync(0m, seller));
    }

    [Fact]
    public async Task OrderTotal_ShouldIncludeServiceFee()
    {
        // Arrange
        var seller = new Seller { Id = Guid.NewGuid(), StoreName = "Test Store", CommissionRate = 12 };
        _mockPlatformFeeService
            .Setup(x => x.GetCommissionRateAsync(seller.Id, It.IsAny<decimal>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(12);

        decimal productAmount = 1000m;
        decimal shippingFee = 50m;

        // Act
        var (commission, serviceFee, _) = 
            await _commissionService.CalculateFeesAsync(productAmount, seller);

        decimal totalOrderAmount = productAmount + serviceFee + shippingFee;
        decimal platformRevenue = commission + serviceFee;

        // Assert
        Assert.Equal(120m, commission);
        Assert.Equal(25m, serviceFee);
        Assert.Equal(1075m, totalOrderAmount); // 1000 + 25 + 50
        Assert.Equal(145m, platformRevenue); // 120 + 25
    }

    [Fact]
    public async Task SellerNetAmount_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        var seller = new Seller { Id = Guid.NewGuid(), StoreName = "Test Store", CommissionRate = 12 };
        _mockPlatformFeeService
            .Setup(x => x.GetCommissionRateAsync(seller.Id, It.IsAny<decimal>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(12);

        decimal gross = 1000m;

        // Act
        var (commission, _, _) = 
            await _commissionService.CalculateFeesAsync(gross, seller);
        decimal net = gross - commission;

        // Assert
        Assert.Equal(120m, commission);
        Assert.Equal(880m, net); // 1000 - 120
    }
}
