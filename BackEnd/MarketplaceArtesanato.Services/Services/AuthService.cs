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
                Country = dto.Address.Country ?? "Brasil",
                Complement = dto.Address.Complement,
                District = dto.Address.District
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
                IsEmailConfirmed = false,
                EmailConfirmationToken = Guid.NewGuid().ToString(),
                EmailConfirmationTokenExpires = DateTime.UtcNow.AddHours(24),
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

                var confirmationLink = $"http://localhost:4200/confirm-email?token={user.EmailConfirmationToken}&email={dto.Email}";
                await _emailService.SendEmailConfirmationAsync(user.Email, user.Name, confirmationLink);
            }
            catch (Exception ex)
            {
                return new AuthResponseDto { Success = false, Message = $"Erro ao salvar: {ex.Message}" };
            }

            var token = GenerateJwtToken(user);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Cadastro realizado com sucesso! Verifique seu email para confirmar sua conta.",
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
                Country = dto.Address.Country ?? "Brasil",
                Complement = dto.Address.Complement,
                District = dto.Address.District
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
                IsEmailConfirmed = false,
                EmailConfirmationToken = Guid.NewGuid().ToString(),
                EmailConfirmationTokenExpires = DateTime.UtcNow.AddHours(24),
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

                var confirmationLink = $"http://localhost:4200/confirm-email?token={user.EmailConfirmationToken}&email={dto.Email}";
                await _emailService.SendEmailConfirmationAsync(user.Email, user.Name, confirmationLink);

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
                .Include(u => u.CustomerProfile)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return new AuthResponseDto { Success = false, Message = "E-mail ou senha inválidos." };

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return new AuthResponseDto { Success = false, Message = "E-mail ou senha inválidos." };
            
            // Validar se cliente está banido
            if (user.Role == UserRole.Customer && user.CustomerProfile?.BannedAt.HasValue == true)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Sua conta foi banida e não pode mais acessar a plataforma."
                };
            }
            
            Console.WriteLine($"Vendedor: {user.Name}, Profile Is Null: {user.SellerProfile == null}, Approved: {user.SellerProfile?.IsApproved}");
            if (user.Role == UserRole.Seller)
            {
                if (user.SellerProfile == null || !user.SellerProfile.IsApproved)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Message = "Sua loja ainda está em análise. Aguarde aprovação do administrador."
                    };
                }
            }

            var token = GenerateJwtToken(user);

            var responseDto = new AuthResponseDto
            {
                Success = true,
                Message = "Login realizado com sucesso!",
                Token = token,
                User = MapToUserDto(user)
            };


            responseDto.User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                ProfileImageUrl = user.ProfileImageUrl,
                Phone = user.Phone,
                CPF = user.CPF,
                Address = user.Address == null ? null : new AddressDto
                {
                    Street = user.Address.Street,
                    Number = user.Address.Number,
                    City = user.Address.City,
                    State = user.Address.State,
                    ZipCode = user.Address.ZipCode,
                    Country = user.Address.Country ?? "Brasil",
                    Complement = user.Address.Complement,
                    District = user.Address.District
                },
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
                Address = user.Address == null ? null : new AddressDto
                {
                    Street = user.Address.Street,
                    Number = user.Address.Number,
                    City = user.Address.City,
                    State = user.Address.State,
                    ZipCode = user.Address.ZipCode,
                    Country = user.Address.Country ?? "Brasil",
                    Complement = user.Address.Complement,
                    District = user.Address.District
                },
                IsApproved = user.IsApproved,
                StoreApproved = user.SellerProfile?.IsApproved
            };
        }

        public async Task<AuthResponseDto> ConfirmEmailAsync(ConfirmEmailDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return new AuthResponseDto { Success = false, Message = "Usuário não encontrado." };

            if (user.IsEmailConfirmed)
                return new AuthResponseDto { Success = true, Message = "Email já foi confirmado." };

            if (user.EmailConfirmationToken != dto.Token || user.EmailConfirmationTokenExpires < DateTime.UtcNow)
                return new AuthResponseDto { Success = false, Message = "Token inválido ou expirado." };

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpires = null;

            await _context.SaveChangesAsync();

            return new AuthResponseDto { Success = true, Message = "Email confirmado com sucesso!" };
        }

        public async Task<AuthResponseDto> ResendConfirmationEmailAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return new AuthResponseDto { Success = false, Message = "Usuário não encontrado." };

            if (user.IsEmailConfirmed)
                return new AuthResponseDto { Success = true, Message = "Email já foi confirmado." };

            user.EmailConfirmationToken = Guid.NewGuid().ToString();
            user.EmailConfirmationTokenExpires = DateTime.UtcNow.AddHours(24);

            await _context.SaveChangesAsync();

            var confirmationLink = $"http://localhost:4200/confirm-email?token={user.EmailConfirmationToken}&email={email}";
            await _emailService.SendEmailConfirmationAsync(user.Email, user.Name, confirmationLink);

            return new AuthResponseDto { Success = true, Message = "Email de confirmação reenviado." };
        }

        private string GenerateSlug(string phrase)
        {
            string str = phrase.ToLower();
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = Regex.Replace(str, @"\s+", " ").Trim();
            str = Regex.Replace(str, @"\s", "-");
            return str;
        }

        public Task<AuthResponseDto> GetCurrentUserAsync()
        {
            throw new NotImplementedException();
        }
    }
}
