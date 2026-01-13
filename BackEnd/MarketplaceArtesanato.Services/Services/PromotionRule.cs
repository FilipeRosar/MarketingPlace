using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
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
    public class PromotionRule : IPriceRule
    {
        private readonly ArtesianDbContext _context;

        public PromotionRule(ArtesianDbContext context)
        {
            _context = context;
        }

        public int Priority => 2;

        public async Task<bool> AppliesAsync(PriceCalculationContext context)
        {
            var now = DateTime.UtcNow;

            var promotions = await _context.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive &&
                            p.StartDate <= now &&
                            p.EndDate >= now)
                .ToListAsync();

            return promotions.Any(p => p.ProductIds.Contains(context.Product.Id));
        }

        public async Task<PriceAdjustment?> CalculateAdjustmentAsync(PriceCalculationContext context)
        {
            var now = DateTime.UtcNow;

            var promotions = await _context.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive &&
                            p.StartDate <= now &&
                            p.EndDate >= now)
                .ToListAsync();

            var promotion = promotions
                .Where(p => p.ProductIds.Contains(context.Product.Id))
                .OrderByDescending(p => p.DiscountPercentage)
                .FirstOrDefault();

            if (promotion == null) return null;

            var discountAmount = context.CurrentPrice * (promotion.DiscountPercentage / 100m);

            return new PriceAdjustment
            {
                Type = PriceAdjustmentType.Promotion,
                Description = promotion.Name,
                Amount = discountAmount,
                Percentage = promotion.DiscountPercentage,
                Priority = Priority
            };
        }
    }
}
