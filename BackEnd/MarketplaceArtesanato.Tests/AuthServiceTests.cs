using System;
using System.Threading.Tasks;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Services.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace MarketplaceArtesanato.Tests.Services;

public class AuthServiceTests
{
    private static ArtesianDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ArtesianDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ArtesianDbContext(options);
    }

    private static IConfiguration CreateMockConfiguration()
    {
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"]).Returns("this-is-a-very-long-secret-key-that-is-at-least-32-characters-long");
        configMock.Setup(c => c["Jwt:Issuer"]).Returns("MarketplaceArtesanato");
        configMock.Setup(c => c["Jwt:Audience"]).Returns("MarketplaceArtesanatoClient");
        return configMock.Object;
    }

    private User CreateTestUser(string email = "test@example.com", string password = "password123")
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = "Test User",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Customer,
            IsApproved = true,
            CreatedAt = DateTime.UtcNow,
            Phone = "123456789",
            CPF = "12345678900"
        };
    }

    #region ForgotPassword Tests

    [Fact]
    public async Task ForgotPasswordAsync_ReturnsSuccess_WhenUserExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ForgotPasswordDto { Email = "test@example.com" };

        // Act
        var result = await authService.ForgotPasswordAsync(dto);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("Email de recuperação enviado", result.Message);
        
        // Verify email service was called
        emailServiceMock.Verify(
            e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_GeneratesValidToken_WhenUserExists()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ForgotPasswordDto { Email = "test@example.com" };

        // Act
        await authService.ForgotPasswordAsync(dto);

        // Assert - Check that token was generated and set
        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(updatedUser?.PasswordResetToken);
        Assert.NotEmpty(updatedUser.PasswordResetToken);
        Assert.True(Guid.TryParse(updatedUser.PasswordResetToken, out _));
    }

    [Fact]
    public async Task ForgotPasswordAsync_SetsTokenExpiration_ToOneHourFromNow()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var beforeTime = DateTime.UtcNow;
        var dto = new ForgotPasswordDto { Email = "test@example.com" };

        // Act
        await authService.ForgotPasswordAsync(dto);
        var afterTime = DateTime.UtcNow;

        // Assert
        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(updatedUser?.ResetTokenExpires);
        var expectedExpiration = beforeTime.AddHours(1);
        var actualExpiration = updatedUser.ResetTokenExpires.Value;
        Assert.True(actualExpiration >= expectedExpiration && actualExpiration <= expectedExpiration.AddSeconds(5),
            $"Token expiration {actualExpiration} should be around {expectedExpiration}");
    }

    [Fact]
    public async Task ForgotPasswordAsync_ReturnsFail_WhenUserNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ForgotPasswordDto { Email = "nonexistent@example.com" };

        // Act
        var result = await authService.ForgotPasswordAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("não encontrado", result.Message, StringComparison.OrdinalIgnoreCase);
        
        // Verify email service was NOT called
        emailServiceMock.Verify(
            e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_CallsEmailService_WithCorrectParameters()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ForgotPasswordDto { Email = "test@example.com" };

        // Act
        await authService.ForgotPasswordAsync(dto);

        // Assert
        emailServiceMock.Verify(
            e => e.SendPasswordResetEmailAsync(
                "test@example.com",
                "Test User",
                It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region ResetPassword Tests

    [Fact]
    public async Task ResetPasswordAsync_SuccessfullyResetsPassword_WithValidToken()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com", "oldPassword123");
        var token = Guid.NewGuid().ToString();
        user.PasswordResetToken = token;
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ResetPasswordDto 
        { 
            Email = "test@example.com",
            Token = token,
            NewPassword = "newPassword123"
        };

        // Act
        var result = await authService.ResetPasswordAsync(dto);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("sucesso", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResetPasswordAsync_UpdatesPasswordHash_WithNewPassword()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com", "oldPassword123");
        var token = Guid.NewGuid().ToString();
        user.PasswordResetToken = token;
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var oldPasswordHash = user.PasswordHash;

        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ResetPasswordDto 
        { 
            Email = "test@example.com",
            Token = token,
            NewPassword = "newPassword123"
        };

        // Act
        await authService.ResetPasswordAsync(dto);

        // Assert
        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotEqual(oldPasswordHash, updatedUser?.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("newPassword123", updatedUser?.PasswordHash));
    }

    [Fact]
    public async Task ResetPasswordAsync_ClearsResetToken_AfterSuccess()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        var token = Guid.NewGuid().ToString();
        user.PasswordResetToken = token;
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ResetPasswordDto 
        { 
            Email = "test@example.com",
            Token = token,
            NewPassword = "newPassword123"
        };

        // Act
        await authService.ResetPasswordAsync(dto);

        // Assert
        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.Null(updatedUser?.PasswordResetToken);
        Assert.Null(updatedUser?.ResetTokenExpires);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsFail_WhenTokenExpired()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        var token = Guid.NewGuid().ToString();
        user.PasswordResetToken = token;
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(-1); // Expired 1 hour ago
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ResetPasswordDto 
        { 
            Email = "test@example.com",
            Token = token,
            NewPassword = "newPassword123"
        };

        // Act
        var result = await authService.ResetPasswordAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Token inválido ou expirado", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsFail_WhenTokenInvalid()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        var token = Guid.NewGuid().ToString();
        user.PasswordResetToken = token;
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ResetPasswordDto 
        { 
            Email = "test@example.com",
            Token = Guid.NewGuid().ToString(), // Wrong token
            NewPassword = "newPassword123"
        };

        // Act
        var result = await authService.ResetPasswordAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Token inválido ou expirado", result.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_ReturnsFail_WhenUserNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ResetPasswordDto 
        { 
            Email = "nonexistent@example.com",
            Token = Guid.NewGuid().ToString(),
            NewPassword = "newPassword123"
        };

        // Act
        var result = await authService.ResetPasswordAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("não encontrado", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResetPasswordAsync_AcceptsNewPasswordDifferentFromOld()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com", "oldPassword123");
        var token = Guid.NewGuid().ToString();
        user.PasswordResetToken = token;
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ResetPasswordDto 
        { 
            Email = "test@example.com",
            Token = token,
            NewPassword = "completelyNewPassword123"
        };

        // Act
        var result = await authService.ResetPasswordAsync(dto);

        // Assert
        Assert.True(result.Success);
        
        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.False(BCrypt.Net.BCrypt.Verify("oldPassword123", updatedUser?.PasswordHash));
        Assert.True(BCrypt.Net.BCrypt.Verify("completelyNewPassword123", updatedUser?.PasswordHash));
    }

    #endregion

    #region ConfirmEmail Tests

    [Fact]
    public async Task ConfirmEmailAsync_SuccessfullyConfirmsEmail_WithValidToken()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        var token = Guid.NewGuid().ToString();
        user.EmailConfirmationToken = token;
        user.EmailConfirmationTokenExpires = DateTime.UtcNow.AddHours(24);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ConfirmEmailDto 
        { 
            Email = "test@example.com",
            Token = token
        };

        // Act
        var result = await authService.ConfirmEmailAsync(dto);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("sucesso", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmEmailAsync_SetsIsEmailConfirmed_ToTrue()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        var token = Guid.NewGuid().ToString();
        user.EmailConfirmationToken = token;
        user.EmailConfirmationTokenExpires = DateTime.UtcNow.AddHours(24);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ConfirmEmailDto 
        { 
            Email = "test@example.com",
            Token = token
        };

        // Act
        await authService.ConfirmEmailAsync(dto);

        // Assert
        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.True(updatedUser?.IsEmailConfirmed);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ClearsConfirmationToken_AfterSuccess()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        var token = Guid.NewGuid().ToString();
        user.EmailConfirmationToken = token;
        user.EmailConfirmationTokenExpires = DateTime.UtcNow.AddHours(24);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ConfirmEmailDto 
        { 
            Email = "test@example.com",
            Token = token
        };

        // Act
        await authService.ConfirmEmailAsync(dto);

        // Assert
        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.Null(updatedUser?.EmailConfirmationToken);
        Assert.Null(updatedUser?.EmailConfirmationTokenExpires);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsFail_WhenTokenExpired()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        var token = Guid.NewGuid().ToString();
        user.EmailConfirmationToken = token;
        user.EmailConfirmationTokenExpires = DateTime.UtcNow.AddHours(-1); // Expired
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ConfirmEmailDto 
        { 
            Email = "test@example.com",
            Token = token
        };

        // Act
        var result = await authService.ConfirmEmailAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("inválido ou expirado", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsFail_WhenTokenInvalid()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        var token = Guid.NewGuid().ToString();
        user.EmailConfirmationToken = token;
        user.EmailConfirmationTokenExpires = DateTime.UtcNow.AddHours(24);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ConfirmEmailDto 
        { 
            Email = "test@example.com",
            Token = Guid.NewGuid().ToString() // Wrong token
        };

        // Act
        var result = await authService.ConfirmEmailAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("inválido ou expirado", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmEmailAsync_ReturnsFail_WhenUserNotFound()
    {
        // Arrange
        using var context = CreateContext();
        var emailServiceMock = new Mock<IEmailService>();
        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        var dto = new ConfirmEmailDto 
        { 
            Email = "nonexistent@example.com",
            Token = Guid.NewGuid().ToString()
        };

        // Act
        var result = await authService.ConfirmEmailAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("não encontrado", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_GeneratesNewToken()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        var oldToken = Guid.NewGuid().ToString();
        user.EmailConfirmationToken = oldToken;
        user.EmailConfirmationTokenExpires = DateTime.UtcNow.AddHours(24);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock
            .Setup(e => e.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        // Act
        var result = await authService.ResendConfirmationEmailAsync("test@example.com");

        // Assert
        Assert.True(result.Success);
        
        var updatedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotEqual(oldToken, updatedUser?.EmailConfirmationToken);
        Assert.NotNull(updatedUser?.EmailConfirmationToken);
    }

    [Fact]
    public async Task ResendConfirmationEmailAsync_CallsEmailService()
    {
        // Arrange
        using var context = CreateContext();
        var user = CreateTestUser("test@example.com");
        user.EmailConfirmationToken = Guid.NewGuid().ToString();
        user.EmailConfirmationTokenExpires = DateTime.UtcNow.AddHours(24);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock
            .Setup(e => e.SendEmailConfirmationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var hubContextMock = new Mock<Microsoft.AspNetCore.SignalR.IHubContext<MarketplaceArtesanato.Core.Hubs.NotificationHub>>();
        var authService = new AuthService(context, CreateMockConfiguration(), hubContextMock.Object, emailServiceMock.Object);

        // Act
        await authService.ResendConfirmationEmailAsync("test@example.com");

        // Assert
        emailServiceMock.Verify(
            e => e.SendEmailConfirmationAsync(
                "test@example.com",
                "Test User",
                It.IsAny<string>()),
            Times.Once);
    }

    #endregion
}
