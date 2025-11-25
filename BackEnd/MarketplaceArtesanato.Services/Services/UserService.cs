using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceArtesanato.Services.Services
{
    public class UserService : IUserService
    {
        private readonly ArtesianDbContext _context;

        public UserService(ArtesianDbContext context)
        {
            _context = context;
        }

        public async Task<bool> UpdateProfileImageAsync(Guid userId, string role, string imageUrl)
        {
            if (role == "Seller")
            {
                var seller = await _context.Sellers.FindAsync(userId);
                if (seller == null) return false;
                seller.ProfileImageUrl = imageUrl;
            }
            else
            {
                var customer = await _context.Customers.FindAsync(userId);
                if (customer == null) return false;
                customer.ProfileImageUrl = imageUrl;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, string role, UpdateUserDto dto)
        {
            if (role == "Seller")
            {
                var seller = await _context.Sellers
                    .Include(s => s.Address)
                    .FirstOrDefaultAsync(s => s.Id == userId);

                if (seller == null) return false;

                seller.Name = dto.Name;
                seller.Phone = dto.Phone;

                if (seller.Address != null)
                {
                    seller.Address.Street = dto.Address.Street;
                    seller.Address.Number = dto.Address.Number;
                    seller.Address.City = dto.Address.City;
                    seller.Address.State = dto.Address.State;
                    seller.Address.ZipCode = dto.Address.ZipCode;
                    seller.Address.Country = dto.Address.Country;
                }
            }
            else
            {
                var customer = await _context.Customers
                    .Include(c => c.Address)
                    .FirstOrDefaultAsync(c => c.Id == userId);

                if (customer == null) return false;

                customer.Name = dto.Name;
                customer.Phone = dto.Phone;

                if (customer.Address != null)
                {
                    customer.Address.Street = dto.Address.Street;
                    customer.Address.Number = dto.Address.Number;
                    customer.Address.City = dto.Address.City;
                    customer.Address.State = dto.Address.State;
                    customer.Address.ZipCode = dto.Address.ZipCode;
                    customer.Address.Country = dto.Address.Country;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}