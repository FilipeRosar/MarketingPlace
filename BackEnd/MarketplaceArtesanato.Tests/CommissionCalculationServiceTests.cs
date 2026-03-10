using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Services.Services;
using Moq;
using Xunit;

namespace MarketplaceArtesanato.Tests;

public class CommissionCalculationServiceTests
{
    private readonly Mock<ISellerSubscriptionService> _mockSubscriptionService;
    private readonly Mock<IPlatformFeeService> _mockPlatformFeeService;
    private readonly CommissionCalculationService _service;

    public CommissionCalculationServiceTests()
    {
        _mockSubscriptionService = new Mock<ISellerSubscriptionService>();
        _mockPlatformFeeService = new Mock<IPlatformFeeService>();
        _service = new CommissionCalculationService(
            _mockSubscriptionService.Object,
            _mockPlatformFeeService.Object);
    }

    [Fact]
    public async Task CalculateFeesAsync_ShouldCalculateCorrectly_WithDefaultRates()
    {
        // Arrange
        var seller = new Seller
        {
            Id = Guid.NewGuid(),
            StoreName = "Test Store",
            CommissionRate = 12
        };
        decimal orderTotal = 1000m;
        
        _mockPlatformFeeService
            .Setup(x => x.GetCommissionRateAsync(seller.Id, It.IsAny<decimal>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(12); // 12% commission

        // Act
        var (commission, serviceFee, platformRevenue) = 
            await _service.CalculateFeesAsync(orderTotal, seller);

        // Assert
        Assert.Equal(120m, commission); // 1000 * 0.12
        Assert.Equal(25m, serviceFee); // 1000 * 0.025
        Assert.Equal(145m, platformRevenue); // 120 + 25
    }

    [Fact]
    public async Task CalculateFeesAsync_ShouldCalculateCorrectly_WithPremiumRates()
    {
        // Arrange
        var seller = new Seller
        {
            Id = Guid.NewGuid(),
            StoreName = "Premium Store",
            CommissionRate = 9
        };
        decimal orderTotal = 2000m;
        
        _mockPlatformFeeService
            .Setup(x => x.GetCommissionRateAsync(seller.Id, It.IsAny<decimal>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(9); // 9% commission (premium)

        // Act
        var (commission, serviceFee, platformRevenue) = 
            await _service.CalculateFeesAsync(orderTotal, seller);

        // Assert
        Assert.Equal(180m, commission); // 2000 * 0.09
        Assert.Equal(50m, serviceFee); // 2000 * 0.025
        Assert.Equal(230m, platformRevenue); // 180 + 50
    }

    [Fact]
    public async Task CalculateFeesAsync_ShouldCalculateCorrectly_WithEliteRates()
    {
        // Arrange
        var seller = new Seller
        {
            Id = Guid.NewGuid(),
            StoreName = "Elite Store",
            CommissionRate = 5
        };
        decimal orderTotal = 5000m;
        
        _mockPlatformFeeService
            .Setup(x => x.GetCommissionRateAsync(seller.Id, It.IsAny<decimal>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(5); // 5% commission (elite)

        // Act
        var (commission, serviceFee, platformRevenue) = 
            await _service.CalculateFeesAsync(orderTotal, seller);

        // Assert
        Assert.Equal(250m, commission); // 5000 * 0.05
        Assert.Equal(125m, serviceFee); // 5000 * 0.025
        Assert.Equal(375m, platformRevenue); // 250 + 125
    }

    [Fact]
    public async Task CalculateFeesAsync_ShouldThrow_WhenSellerIsNull()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.CalculateFeesAsync(1000m, null!));
    }

    [Fact]
    public async Task CalculateFeesAsync_ShouldThrow_WhenOrderTotalIsNegative()
    {
        // Arrange
        var seller = new Seller
        {
            Id = Guid.NewGuid(),
            StoreName = "Test Store"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CalculateFeesAsync(-100m, seller));
    }

    [Fact]
    public async Task GetServiceFeePercentageAsync_ShouldReturnDefaultPercentage()
    {
        // Act
        var percentage = await _service.GetServiceFeePercentageAsync();

        // Assert
        Assert.Equal(0.025m, percentage); // 2.5%
    }

    [Fact]
    public async Task CalculateFeesAsync_ShouldCallGetCommissionRateAsync()
    {
        // Arrange
        var seller = new Seller
        {
            Id = Guid.NewGuid(),
            StoreName = "Test Store"
        };
        decimal orderTotal = 1000m;
        
        _mockPlatformFeeService
            .Setup(x => x.GetCommissionRateAsync(seller.Id, It.IsAny<decimal>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(12);

        // Act
        await _service.CalculateFeesAsync(orderTotal, seller);

        // Assert
        _mockPlatformFeeService.Verify(
            x => x.GetCommissionRateAsync(seller.Id, It.IsAny<decimal>(), It.IsAny<DateTime?>()),
            Times.Once);
    }

    [Fact]
    public async Task CalculateFeesAsync_ShouldCalculateSmallOrders()
    {
        // Arrange
        var seller = new Seller
        {
            Id = Guid.NewGuid(),
            StoreName = "Test Store"
        };
        decimal orderTotal = 10m;
        
        _mockPlatformFeeService
            .Setup(x => x.GetCommissionRateAsync(seller.Id, It.IsAny<decimal>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(12);

        // Act
        var (commission, serviceFee, platformRevenue) = 
            await _service.CalculateFeesAsync(orderTotal, seller);

        // Assert
        Assert.Equal(1.2m, commission); // 10 * 0.12
        Assert.Equal(0.25m, serviceFee); // 10 * 0.025
        Assert.Equal(1.45m, platformRevenue); // 1.2 + 0.25
    }

    [Fact]
    public async Task CalculateFeesAsync_ShouldCalculateLargeOrders()
    {
        // Arrange
        var seller = new Seller
        {
            Id = Guid.NewGuid(),
            StoreName = "Test Store"
        };
        decimal orderTotal = 100000m;
        
        _mockPlatformFeeService
            .Setup(x => x.GetCommissionRateAsync(seller.Id, It.IsAny<decimal>(), It.IsAny<DateTime?>()))
            .ReturnsAsync(12);

        // Act
        var (commission, serviceFee, platformRevenue) = 
            await _service.CalculateFeesAsync(orderTotal, seller);

        // Assert
        Assert.Equal(12000m, commission); // 100000 * 0.12
        Assert.Equal(2500m, serviceFee); // 100000 * 0.025
        Assert.Equal(14500m, platformRevenue); // 12000 + 2500
    }
}
