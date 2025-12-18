using BCrypt.Net;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace MarketplaceArtesanato.Data.Seed;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ArtesianDbContext>();

        await context.Database.MigrateAsync();

        if (await context.Users.AnyAsync(u => u.Email == "admin@trama.com"))
        {
            Console.WriteLine("Seed já aplicado. Base de dados pronta.");
            return;
        }

        Console.WriteLine("Iniciando Seed do Admin...");

        var adminAddress = new Address
        {
            Id = Guid.NewGuid(),
            Street = "Avenida Principal",
            Number = "1000",
            City = "São Paulo",
            State = "SP",
            ZipCode = "01000-000",
            Country = "Brasil"
        };

        context.Addresses.Add(adminAddress);
        await context.SaveChangesAsync();

        var userAdmin = new User
        {
            Id = Guid.NewGuid(),
            Name = "Administrador Trama",
            Email = "admin@trama.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.Admin,
            Phone = "(11) 99999-9999",
            CPF = "000.000.000-00", 
            IsApproved = true,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,

            AddressId = adminAddress.Id
        };

        context.Users.Add(userAdmin);
        await context.SaveChangesAsync();

        var sellerAdmin = new Seller
        {
            Id = Guid.NewGuid(),
            UserId = userAdmin.Id, 

            StoreName = "Loja Admin Trama", 
            StoreSlug = "loja-admin-trama", 
            Bio = "Loja oficial do administrador do sistema Trama Artesanato.",

            IsApproved = true,  
            CommissionRate = 0, 
            RatingAverage = 5.0m,

            CreatedAt = DateTime.UtcNow,
            IsDeleted = false,

            AddressId = adminAddress.Id
        };

        context.Sellers.Add(sellerAdmin);

        var adminProfile = new Admin
        {
            Id = Guid.NewGuid(),
            UserId = userAdmin.Id,
            InternalCode = "ADM-001",
            Department = "Tecnologia"
        };

        context.Admins.Add(adminProfile);

        await context.SaveChangesAsync();

        Console.WriteLine("==============================================");
        Console.WriteLine("USUÁRIO & VENDEDOR ADMIN CRIADOS COM SUCESSO!");
        Console.WriteLine($"User ID: {userAdmin.Id}");
        Console.WriteLine("Email: admin@trama.com");
        Console.WriteLine("Senha: Admin123!");
        Console.WriteLine("==============================================");
    }
}