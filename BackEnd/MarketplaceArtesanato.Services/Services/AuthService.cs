using BCrypt.Net;
using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums; // Importante
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

        // --- LOGIN ---
        public async Task<AuthResponseDto> LoginAsync(LoginDto request)
        {
            var customer = await _context.Customers
                .Include(c => c.Address)
                .FirstOrDefaultAsync(c => c.Email == request.Email);

            var seller = await _context.Sellers
                .Include(s => s.Address)
                .FirstOrDefaultAsync(s => s.Email == request.Email);

            object? user = customer ?? (object?)seller;

            string roleString = "";

            if (customer != null)
            {
                roleString = customer.Role.ToString(); 
            }
            else if (seller != null)
            {
                roleString = seller.Role.ToString(); 
            }

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, GetPasswordHash(user)))
            {
                return new AuthResponseDto { Success = false, Message = "Credenciais inválidas." };
            }

            var userId = GetUserId(user);
            var userName = GetUserName(user);

            var token = GenerateJwtToken(userId, roleString, userName, request.Email);


            var userDto = MapToUserDto(user);
            userDto.Role = Enum.TryParse<UserRole>(roleString, out var parsedRole) ? parsedRole : UserRole.Customer;

            return new AuthResponseDto
            {
                Success = true,
                Token = token,
                User = userDto
            };
        }

        public async Task<AuthResponseDto> RegisterCustomerAsync(RegisterCostumerDto dto)
        {
            if (await _context.Customers.AnyAsync(c => c.Email == dto.Email) ||
                await _context.Sellers.AnyAsync(s => s.Email == dto.Email))
                return new AuthResponseDto { Success = false, Message = "E-mail já está em uso." };


            var address = new Address
            {
                Street = dto.Address.Street,
                Number = dto.Address.Number,
                City = dto.Address.City,
                State = dto.Address.State,
                ZipCode = dto.Address.ZipCode,
                Country = dto.Address.Country ?? "Brasil"
            };

            var customer = new Customer
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone,
                CPF = dto.CPF,
                AddressId = address.Id,
                Address = address,
                Role = UserRole.Customer, 
            };

            try
            {
                _context.Addresses.Add(address);
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return new AuthResponseDto { Success = false, Message = $"Erro ao salvar: {ex.Message}" };
            }

            var token = GenerateJwtToken(customer.Id, "Customer", customer.Name, customer.Email);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Conta criada com sucesso!",
                Token = token,
                User = MapToUserDto(customer)
            };
        }

        public async Task<AuthResponseDto> RegisterSellerAsync(RegisterSellerDto dto)
        {
            if (await _context.Sellers.AnyAsync(s => s.Email == dto.Email))
                return new AuthResponseDto { Success = false, Message = "E-mail já está em uso." };

            var address = new Address
            {
                Street = dto.Address.Street,
                Number = dto.Address.Number,
                City = dto.Address.City,
                State = dto.Address.State,
                ZipCode = dto.Address.ZipCode,
                Country = dto.Address.Country ?? "Brasil"
            };

            var seller = new Seller
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone,
                CPF = dto.CPF,
                CNPJ = dto.CNPJ,
                AddressId = address.Id,
                Address = address,
                Role = UserRole.Seller, 
            };

            try
            {
                _context.Addresses.Add(address);
                _context.Sellers.Add(seller);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return new AuthResponseDto { Success = false, Message = $"Erro ao salvar: {ex.Message}" };
            }

            var token = GenerateJwtToken(seller.Id, "Seller", seller.Name, seller.Email);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Conta criada com sucesso!",
                Token = token,
                User = MapToUserDto(seller)
            };
        }

        // --- HELPERS ---
        private string GenerateJwtToken(Guid userId, string role, string name, string email)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role), 
                new Claim(ClaimTypes.Name, name),
                new Claim("UserType", role)
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

        private UserDto MapToUserDto(dynamic user)
        {
            string? imageUrl = null;
            UserRole roleEnum = UserRole.Customer;

            if (user is Customer c)
            {
                imageUrl = c.ProfileImageUrl;
                roleEnum = c.Role;
            }
            else if (user is Seller s)
            {
                imageUrl = s.ProfileImageUrl;
                roleEnum = s.Role; 
            }

            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = roleEnum,
                ProfileImageUrl = imageUrl,
                Phone = user.Phone,
                CPF = user.CPF,
            };
        }

        private string GetPasswordHash(object user) => user is Customer c ? c.PasswordHash : ((Seller)user).PasswordHash;
        private Guid GetUserId(object user) => user is Customer c ? c.Id : ((Seller)user).Id;
        private string GetUserName(object user) => user is Customer c ? c.Name : ((Seller)user).Name;
    }
}