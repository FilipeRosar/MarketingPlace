using BCrypt.Net;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Identity;
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

        // Garante que o banco existe
        await context.Database.MigrateAsync();

        // Verifica se já existe admin
        if (await context.Sellers.AnyAsync(s => s.Email == "admin@trama.com"))
        {
            Console.WriteLine("Admin já existe. Pulando seed.");
            return;
        }

        // CRIA ENDEREÇO PARA O ADMIN
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

        var admin = new Seller
        {
            Id = Guid.NewGuid(),
            Name = "Administrador Trama",
            Email = "admin@trama.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Role = UserRole.Admin,
            isAproved = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false,
            Bio = "Administrador do sistema Trama Artesanato",
            Phone = "(11) 99999-9999",
            AddressId = adminAddress.Id,  
            Address = adminAddress        
        };

        context.Sellers.Add(admin);
        await context.SaveChangesAsync();

        Console.WriteLine("==============================================");
        Console.WriteLine("USUÁRIO ADMIN CRIADO COM SUCESSO!");
        Console.WriteLine("Email: admin@trama.com");
        Console.WriteLine("Senha: Admin123!");
        Console.WriteLine("Role: Admin");
        Console.WriteLine("==============================================");
    }
}