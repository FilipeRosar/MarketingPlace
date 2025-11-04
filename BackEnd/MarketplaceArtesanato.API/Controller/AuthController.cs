using BCrypt.Net;
using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ArtesianDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ArtesianDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register/customer")]
        public async Task<ActionResult> RegisterCustomer(RegisterCostumerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (await _context.Customers.AnyAsync(c => c.Email == dto.Email))
                return Conflict("Email already in use.");


            var address = new Address
            {
                Id = Guid.NewGuid(),
                Street = dto.Address.Street,
                Number = dto.Address.Number,
                City = dto.Address.City,
                State = dto.Address.State,
                ZipCode = dto.Address.ZipCode,
                Country = dto.Address.Country
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone,
                CPF = dto.CPF,
                AddressId = address.Id,
                Address = address,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(customer.Id, "Customer", customer.Name, customer.Email);

            return Ok(new
            {
                message = "Customer created successfully",
                token,
                user = new
                {
                    customer.Id,
                    customer.Name,
                    customer.Email,
                    Role = UserRole.Customer,
                    customer.Phone,
                    customer.CPF
                }
            });
        }

        [HttpPost("register/seller")]
        public async Task<ActionResult> RegisterSeller(RegisterSellerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (await _context.Seller.AnyAsync(s => s.Email == dto.Email))
                return Conflict("Email already in use.");

            if (string.IsNullOrWhiteSpace(dto.CPF) && string.IsNullOrWhiteSpace(dto.CNPJ))
                return BadRequest("CPF ou CNPJ é obrigatório.");

            var address = new Address
            {
                Id = Guid.NewGuid(),
                Street = dto.Address.Street,
                Number = dto.Address.Number,
                City = dto.Address.City,
                State = dto.Address.State,
                ZipCode = dto.Address.ZipCode,
                Country = dto.Address.Country
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            var seller = new Seller
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone,
                CPF = dto.CPF,
                CNPJ = dto.CNPJ,
                AddressId = address.Id,
                Address = address,
                CreatedAt = DateTime.UtcNow
            };

            _context.Seller.Add(seller);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(seller.Id, "Seller", seller.Name, seller.Email);

            return Ok(new
            {
                message = "Seller created successfully",
                token,
                user = new
                {
                    seller.Id,
                    seller.Name,
                    seller.Email,
                    Role =UserRole.Seller,
                    seller.Phone,
                    seller.CPF,
                    seller.CNPJ
                }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Busca em Customers
            var customer = await _context.Customers
                .Include(c => c.Address)
                .FirstOrDefaultAsync(c => c.Email == dto.Email);

            // Busca em Sellers
            var seller = await _context.Seller
                .Include(s => s.Address)
                .FirstOrDefaultAsync(s => s.Email == dto.Email);

            object? user = customer ?? (object?)seller;
            string role = customer != null ? "Customer" : (seller != null ? "Seller" : "");

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, GetPasswordHash(user)))
            {
                return Unauthorized(new { message = "Credenciais inválidas." });
            }

            var userId = GetUserId(user);
            var userName = GetUserName(user);

            var token = GenerateJwtToken(userId, role, userName, dto.Email);

            return Ok(new
            {
                message = "Login bem-sucedido.",
                token,
                expiresIn = 7200,
                user = new
                {
                    Id = userId,
                    Name = userName,
                    Email = dto.Email,
                    Role = role,
                    Phone = GetUserPhone(user)
                }
            });
        }

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
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Helpers
        private string GetPasswordHash(object user) => user is Customer c ? c.PasswordHash : ((Seller)user).PasswordHash;
        private Guid GetUserId(object user) => user is Customer c ? c.Id : ((Seller)user).Id;
        private string GetUserName(object user) => user is Customer c ? c.Name : ((Seller)user).Name;
        private string? GetUserPhone(object user) => user is Customer c ? c.Phone : ((Seller)user).Phone;

        
    }
}