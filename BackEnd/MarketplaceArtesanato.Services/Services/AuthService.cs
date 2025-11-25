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
using StackExchange.Redis;
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
            // 1. Tenta achar em Customers
            var customer = await _context.Customers
                .Include(c => c.Address)
                .FirstOrDefaultAsync(c => c.Email == request.Email);

            // 2. Tenta achar em Sellers
            var seller = await _context.Sellers
                .Include(s => s.Address)
                .FirstOrDefaultAsync(s => s.Email == request.Email);

            // Unifica o objeto usuário
            object? user = customer ?? (object?)seller;
            string role = customer != null ? "Customer" : (seller != null ? "Seller" : "");

            // 3. Valida Senha
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, GetPasswordHash(user)))
            {
                return new AuthResponseDto { Success = false, Message = "Credenciais inválidas." };
            }

            // 4. Gera Token
            var userId = GetUserId(user);
            var userName = GetUserName(user);
            var token = GenerateJwtToken(userId, role, userName, request.Email);

            return new AuthResponseDto
            {
                Success = true,
                Token = token,
                User = MapToUserDto(user)
            };
        }

        // --- REGISTER CUSTOMER ---
        public async Task<AuthResponseDto> RegisterCustomerAsync(RegisterCostumerDto dto)
        {
            // 1. VALIDAÇÕES DE UNICIDADE (Crucial)
            // Verifica se o e-mail já existe em QUALQUER tabela (Cliente ou Vendedor)
            if (await _context.Customers.AnyAsync(c => c.Email == dto.Email) ||
                await _context.Sellers.AnyAsync(s => s.Email == dto.Email))
                return new AuthResponseDto { Success = false, Message = "Este e-mail já está registrado." };

            // Verifica se o CPF já existe em QUALQUER tabela
            if (await _context.Customers.AnyAsync(c => c.CPF == dto.CPF) ||
                await _context.Sellers.AnyAsync(s => s.CPF == dto.CPF))
                return new AuthResponseDto { Success = false, Message = "Este CPF já está registrado." };

            // 2. Cria o Endereço
            var address = new Address
            {
                Street = dto.Address.Street,
                Number = dto.Address.Number,
                City = dto.Address.City,
                State = dto.Address.State,
                ZipCode = dto.Address.ZipCode,
                Country = dto.Address.Country ?? "Brasil"
            };

            // 3. Cria o Cliente
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
                return new AuthResponseDto { Success = false, Message = $"Erro ao salvar no banco: {ex.Message}" };
            }

            // 4. Gera Token (Login Automático)
            var token = GenerateJwtToken(customer.Id, "Customer", customer.Name, customer.Email);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Conta criada com sucesso!",
                Token = token,
                User = MapToUserDto(customer)
            };
        }

        // --- REGISTER SELLER ---
        public async Task<AuthResponseDto> RegisterSellerAsync(RegisterSellerDto dto)
        {
            // 1. VALIDAÇÕES DE UNICIDADE
            if (await _context.Sellers.AnyAsync(s => s.Email == dto.Email) ||
                await _context.Customers.AnyAsync(c => c.Email == dto.Email))
                return new AuthResponseDto { Success = false, Message = "Este e-mail já está registrado." };

            if (!string.IsNullOrWhiteSpace(dto.CPF) &&
                (await _context.Sellers.AnyAsync(s => s.CPF == dto.CPF) || await _context.Customers.AnyAsync(c => c.CPF == dto.CPF)))
                return new AuthResponseDto { Success = false, Message = "Este CPF já está registrado." };

            if (!string.IsNullOrWhiteSpace(dto.CNPJ) &&
                await _context.Sellers.AnyAsync(s => s.CNPJ == dto.CNPJ))
                return new AuthResponseDto { Success = false, Message = "Este CNPJ já está registrado." };

            var address = new Address
            {
                Street = dto.Address.Street,
                Number = dto.Address.Number,
                City = dto.Address.City,
                State = dto.Address.State,
                ZipCode = dto.Address.ZipCode,
                Country = dto.Address.Country ?? "Brasil"
            };

            // 3. Criar Vendedor
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

            // 4. Gera Token (Login Automático)
            var token = GenerateJwtToken(seller.Id, "Seller", seller.Name, seller.Email);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Conta de vendedor criada com sucesso!",
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

            if (user is Customer c) imageUrl = c.ProfileImageUrl;
            else if (user is Seller s) imageUrl = s.ProfileImageUrl;

            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
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