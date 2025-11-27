using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class FavoritesService : IFavoritesService
    {
        private readonly ArtesianDbContext _context;

        public FavoritesService(ArtesianDbContext context)
        {
            _context = context;
        }

        public async Task<List<Guid>> GetFavoriteProductIdsAsync(Guid userId)
        {
            return await _context.UserFavorites
                .Where(f => f.UserId == userId)
                .Select(f => f.ProductId)
                .ToListAsync();
        }

        public async Task AddToFavoritesAsync(Guid userId, Guid productId)
        {
            var exists = await _context.UserFavorites
                .AnyAsync(f => f.UserId == userId && f.ProductId == productId);

            if (!exists)
            {
                var favorite = new UserFavorite
                {
                    UserId = userId,
                    ProductId = productId
                };
                _context.UserFavorites.Add(favorite);
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromFavoritesAsync(Guid userId, Guid productId)
        {
            var favorite = await _context.UserFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (favorite != null)
            {
                _context.UserFavorites.Remove(favorite);
                await _context.SaveChangesAsync();
            }
        }
    }
}
