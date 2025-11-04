using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MarketplaceArtesanato.Data.Data;
using Microsoft.IdentityModel.Tokens;  
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;

namespace MarketplaceArtesanato.Services.Services
{
    public class AuthService
    {
        private readonly ArtesianDbContext _context;
        private readonly IConfiguration _configuration;
        public AuthService(ArtesianDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        public async Task<bool> IsEmailRegisteredAsync(string email)
        {
            return await _context.Seller.AnyAsync(u => u.Email == email);
        }

    }
}
