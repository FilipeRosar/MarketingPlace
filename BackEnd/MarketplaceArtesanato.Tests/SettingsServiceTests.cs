using System;
using System.Threading.Tasks;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Services.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MarketplaceArtesanato.Tests.Services;

public class SettingsServiceTests
{
    private static ArtesianDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ArtesianDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ArtesianDbContext(options);
    }

    [Fact]
    public async Task GetCommissionRateAsync_ReturnsDefault_WhenMissing()
    {
        using var context = CreateContext();
        var service = new SettingsService(context);

        var rate = await service.GetCommissionRateAsync();

        Assert.Equal(0.15m, rate);
    }

    [Fact]
    public async Task GetCommissionRateAsync_ReturnsStoredValue_WhenPresent()
    {
        using var context = CreateContext();
        context.SystemSettings.Add(new SystemSetting
        {
            Id = Guid.NewGuid(),
            Key = "PlatformCommissionRate",
            Value = "0.20"
        });
        await context.SaveChangesAsync();

        var service = new SettingsService(context);

        var rate = await service.GetCommissionRateAsync();

        Assert.Equal(0.20m, rate);
    }

    [Fact]
    public async Task UpdateSettingAsync_CreatesSetting_WhenMissing()
    {
        using var context = CreateContext();
        var service = new SettingsService(context);

        await service.UpdateSettingAsync("ServiceFee", "3.50");

        var setting = await context.SystemSettings.SingleAsync(s => s.Key == "ServiceFee");
        Assert.Equal("3.50", setting.Value);
    }

    [Fact]
    public async Task UpdateSettingAsync_UpdatesSetting_WhenExists()
    {
        using var context = CreateContext();
        context.SystemSettings.Add(new SystemSetting
        {
            Id = Guid.NewGuid(),
            Key = "PlatformCommissionRate",
            Value = "0.10"
        });
        await context.SaveChangesAsync();

        var service = new SettingsService(context);

        await service.UpdateSettingAsync("PlatformCommissionRate", "0.25");

        var setting = await context.SystemSettings.SingleAsync(s => s.Key == "PlatformCommissionRate");
        Assert.Equal("0.25", setting.Value);
        Assert.NotNull(setting.UpdatedAt);
    }
}
