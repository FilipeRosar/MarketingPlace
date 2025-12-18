using BCrypt.Net;
using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MarketplaceArtesanato.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly ArtesianDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthService(ArtesianDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto request)
        {
            var user = await _context.Users
                .Include(u => u.SellerProfile)
                .Include(u => u.CustomerProfile)
                .Include(u => u.Address) 
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return new AuthResponseDto { Success = false, Message = "Credenciais inválidas." };
            }

            var token = GenerateJwtToken(user);
            var userDto = MapToUserDto(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login realizado com sucesso!",
                Token = token,
                User = userDto
            };
        }

        public async Task<AuthResponseDto> RegisterCustomerAsync(RegisterCostumerDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return new AuthResponseDto { Success = false, Message = "E-mail já está em uso." };

            var address = new Address
            {
                Id = Guid.NewGuid(),
                Street = dto.Address.Street,
                Number = dto.Address.Number,
                City = dto.Address.City,
                State = dto.Address.State,
                ZipCode = dto.Address.ZipCode,
                Country = dto.Address.Country ?? "Brasil"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone,
                CPF = dto.CPF,
                Role = UserRole.Customer,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow,
                Address = address,
                AddressId = address.Id
            };

            var customerProfile = new Customer
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                LoyaltyPoints = 0,
                BirthDate = dto.BirthDate
            };

            try
            {
                _context.Users.Add(user);
                _context.Customers.Add(customerProfile);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return new AuthResponseDto { Success = false, Message = $"Erro ao salvar: {ex.Message}" };
            }

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Conta criada com sucesso!",
                Token = token,
                User = MapToUserDto(user)
            };
        }

        public async Task<AuthResponseDto> RegisterSellerAsync(RegisterSellerDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return new AuthResponseDto { Success = false, Message = "E-mail já está em uso." };

            var userAddress = new Address
            {
                Id = Guid.NewGuid(),
                Street = dto.Address.Street,
                Number = dto.Address.Number,
                City = dto.Address.City,
                State = dto.Address.State,
                ZipCode = dto.Address.ZipCode,
                Country = dto.Address.Country ?? "Brasil"
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone,
                CPF = dto.CPF,
                Role = UserRole.Seller,
                IsApproved = true, 
                CreatedAt = DateTime.UtcNow,
                Address = userAddress,
                AddressId = userAddress.Id
            };

            string slug = GenerateSlug(dto.Name); 

            var sellerProfile = new Seller
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StoreName = dto.Name, 
                StoreSlug = slug,
                CNPJ = dto.CNPJ,
                Address = userAddress, 
                AddressId = userAddress.Id,
                IsApproved = false, 
                CommissionRate = 15.0m
            };

            try
            {
                _context.Users.Add(user);
                _context.Sellers.Add(sellerProfile);

                var customerProfile = new Customer { Id = Guid.NewGuid(), UserId = user.Id };
                _context.Customers.Add(customerProfile);

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return new AuthResponseDto { Success = false, Message = $"Erro ao salvar: {ex.Message}" };
            }

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Conta criada com sucesso! Aguarde aprovação da loja.",
                Token = token,
                User = MapToUserDto(user)
            };
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("StoreApproved", (user.SellerProfile?.IsApproved ?? false).ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                ProfileImageUrl = user.ProfileImageUrl,
                Phone = user.Phone,
                CPF = user.CPF,
            };
        }

        private string GenerateSlug(string phrase)
        {
            string str = phrase.ToLower();
            return str.Replace(" ", "-").Trim();
        }
    }
}