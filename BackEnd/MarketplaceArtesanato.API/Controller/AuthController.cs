using BCrypt.Net;
using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Hubs;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions; 

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ArtesianDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AuthController(
            ArtesianDbContext context,
            IConfiguration configuration,
            IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _configuration = configuration;
            _hubContext = hubContext;
        }

        [HttpPost("register/customer")]
        public async Task<ActionResult> RegisterCustomer([FromBody] RegisterCostumerDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict("Email já cadastrado.");

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

            _context.Users.Add(user);
            _context.Customers.Add(customerProfile);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Cadastro realizado com sucesso! Bem-vindo à Trama.",
                token,
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role,
                    isApproved = user.IsApproved
                }
            });
        }

        [HttpPost("register/seller")]
        public async Task<ActionResult> RegisterSeller([FromBody] RegisterSellerDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return Conflict("Email já cadastrado.");

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

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

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
                CreatedAt = DateTime.UtcNow
            };

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

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Cadastro realizado com sucesso! Aguarde aprovação do administrador.",
                token,
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role,
                    isApproved = user.IsApproved
                }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Address)
                .Include(u => u.SellerProfile)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Credenciais inválidas.");

            if (user.Role == UserRole.Seller && user.SellerProfile != null && !user.SellerProfile.IsApproved)
            {
                return Unauthorized("Sua loja ainda está em análise.");
            }

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Login realizado com sucesso!",
                token,
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role,
                    user.Phone,
                    isApproved = user.IsApproved,
                    storeApproved = user.SellerProfile?.IsApproved ?? true
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Role, user.Role.ToString()),
                new("role", user.Role.ToString()),
                new("isApproved", user.IsApproved.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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