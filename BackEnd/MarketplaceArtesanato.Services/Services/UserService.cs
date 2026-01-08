using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;

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
            var user = await _context.Users.FindAsync(userId);

            if (user == null) return false;

            user.ProfileImageUrl = imageUrl;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, string role, UpdateUserDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Address) 
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null) return false;

            user.Name = dto.Name;
            user.Phone = dto.Phone;

            if (user.Address != null)
            {
                user.Address.Street = dto.Address.Street;
                user.Address.Number = dto.Address.Number;
                user.Address.City = dto.Address.City;
                user.Address.State = dto.Address.State;
                user.Address.ZipCode = dto.Address.ZipCode;
                user.Address.Country = dto.Address.Country ?? "Brasil";
                user.Address.Complement = dto.Address.Complement;
                user.Address.District = dto.Address.District;
            }
            else
            {
                var newAddress = new Address
                {
                    Id = Guid.NewGuid(),
                    Street = dto.Address.Street,
                    Number = dto.Address.Number,
                    City = dto.Address.City,
                    State = dto.Address.State,
                    ZipCode = dto.Address.ZipCode,
                    Country = dto.Address.Country ?? "Brasil",
                    Complement = dto.Address.Complement,
                    District = dto.Address.District
                };
                user.Address = newAddress;
                _context.Addresses.Add(newAddress);
            }

            if (role == "Seller")
            {
                var seller = await _context.Sellers
                    .Include(s => s.Address)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (seller != null)
                {
                    if (seller.Address != null)
                    {
                        seller.Address.Street = dto.Address.Street;
                        seller.Address.Number = dto.Address.Number;
                        seller.Address.City = dto.Address.City;
                        seller.Address.State = dto.Address.State;
                        seller.Address.ZipCode = dto.Address.ZipCode;
                        seller.Address.Country = dto.Address.Country ?? "Brasil";
                        seller.Address.Complement = dto.Address.Complement;
                        seller.Address.District = dto.Address.District;
                    }
                    else
                    {
                        var sellerAddress = new Address
                        {
                            Id = Guid.NewGuid(),
                            Street = dto.Address.Street,
                            Number = dto.Address.Number,
                            City = dto.Address.City,
                            State = dto.Address.State,
                            ZipCode = dto.Address.ZipCode,
                            Country = dto.Address.Country ?? "Brasil",
                            Complement = dto.Address.Complement,
                            District = dto.Address.District
                        };
                        seller.Address = sellerAddress;
                        _context.Addresses.Add(sellerAddress);
                    }
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
