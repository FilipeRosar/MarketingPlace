using BCrypt.Net;
using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Hubs;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace MarketplaceArtesanato.Services.Services
{
    public class AuthService : IAuthService
    {
        private readonly ArtesianDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IEmailService _emailService;

        public AuthService(
            ArtesianDbContext context,
            IConfiguration configuration,
            IHubContext<NotificationHub> hubContext,
            IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _hubContext = hubContext;
            _emailService = emailService;
        }

        public async Task<AuthResponseDto> RegisterCustomerAsync(RegisterCostumerDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return new AuthResponseDto { Success = false, Message = "Email já cadastrado." };

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
                Role = UserRole.Customer,
                Phone = dto.Phone,
                CPF = dto.CPF,
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
                Message = "Cadastro realizado com sucesso! Bem-vindo à Trama.",
                Token = token,
                User = MapToUserDto(user)
            };
        }

        public async Task<AuthResponseDto> RegisterSellerAsync(RegisterSellerDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return new AuthResponseDto { Success = false, Message = "Email já cadastrado." };

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
                Role = UserRole.Seller,
                Phone = dto.Phone,
                CPF = dto.CPF,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow,
                Address = address,
                AddressId = address.Id
            };

            string slug = GenerateSlug(dto.Name);

            var seller = new Seller
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                StoreName = dto.Name,
                StoreSlug = slug,
                CNPJ = dto.CNPJ,
                Bio = dto.Bio ?? "",
                AddressId = address.Id,
                Address = address,
                IsApproved = false,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                CommissionRate = 15.0m
            };

            try
            {
                _context.Users.Add(user);
                _context.Sellers.Add(seller);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group("Admins")
                    .SendAsync("ReceiveNotification", new
                    {
                        title = "Novo Artesão Cadastrado! 🎨",
                        message = $"{user.Name} está aguardando aprovação.",
                        icon = "🧑‍🎨",
                        type = "info"
                    });
            }
            catch (Exception ex)
            {
                return new AuthResponseDto { Success = false, Message = $"Erro ao salvar: {ex.Message}" };
            }

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Cadastro realizado com sucesso! Aguarde aprovação do administrador.",
                Token = token,
                User = MapToUserDto(user)
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Address)
                .Include(u => u.SellerProfile)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return new AuthResponseDto { Success = false, Message = "Credenciais inválidas." };

            if (user.Role == UserRole.Seller && user.SellerProfile != null && !user.SellerProfile.IsApproved)
            {
                return new AuthResponseDto { Success = false, Message = "Sua loja ainda está em análise." };
            }

            var token = GenerateJwtToken(user);

            var responseDto = new AuthResponseDto
            {
                Success = true,
                Message = "Login realizado com sucesso!",
                Token = token,
                User = MapToUserDto(user)
            };

            // Add store approval status if applicable (this requires AuthResponseDto or UserDto to have this field)
            // Assuming UserDto is flexible or you can add it to the generic User object in AuthResponseDto
            // For now, mapping directly what was in the controller:
            responseDto.User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Phone = user.Phone,
                IsApproved = user.IsApproved, 
                StoreApproved = user.SellerProfile?.IsApproved ?? true 
            };

            return responseDto;
        }

        public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
            {
                return new AuthResponseDto { Success = false, Message = "Usuário não encontrado." };
            }

            var token = Guid.NewGuid().ToString();
            user.PasswordResetToken = token;
            user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();

            var resetLink = $"http://localhost:4200/reset-password?token={token}&email={dto.Email}";
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetLink);

            return new AuthResponseDto { Success = true, Message = "Email de recuperação enviado." };
        }

        public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return new AuthResponseDto { Success = false, Message = "Usuário não encontrado." };

            if (user.PasswordResetToken != dto.Token || user.ResetTokenExpires < DateTime.UtcNow)
            {
                return new AuthResponseDto { Success = false, Message = "Token inválido ou expirado." };
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordResetToken = null;
            user.ResetTokenExpires = null;

            await _context.SaveChangesAsync();

            return new AuthResponseDto { Success = true, Message = "Senha alterada com sucesso!" };
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("role", user.Role.ToString()),
                new Claim("isApproved", user.IsApproved.ToString())
            };

            if (user.SellerProfile != null)
            {
                claims.Add(new Claim("StoreApproved", user.SellerProfile.IsApproved.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
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
                IsApproved = user.IsApproved,
                StoreApproved = user.SellerProfile?.IsApproved
            };
        }

        private string GenerateSlug(string phrase)
        {
            string str = phrase.ToLower();
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = Regex.Replace(str, @"\s+", " ").Trim();
            str = Regex.Replace(str, @"\s", "-");
            return str;
        }
    }
}