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
            var hasPromotion = await _context.Promotions
                .AnyAsync(p => p.IsActive &&
                              p.ProductIds.Contains(context.Product.Id) &&
                              p.StartDate <= DateTime.UtcNow &&
                              p.EndDate >= DateTime.UtcNow);

            return hasPromotion;
        }

        public async Task<PriceAdjustment?> CalculateAdjustmentAsync(PriceCalculationContext context)
        {
            var promotion = await _context.Promotions
                .Where(p => p.IsActive &&
                           p.ProductIds.Contains(context.Product.Id) &&
                           p.StartDate <= DateTime.UtcNow &&
                           p.EndDate >= DateTime.UtcNow)
                .OrderByDescending(p => p.DiscountPercentage)
                .FirstOrDefaultAsync();

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
