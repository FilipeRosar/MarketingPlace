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
    public class CampaignRule : IPriceRule
    {
        private readonly ArtesianDbContext _context;

        public CampaignRule(ArtesianDbContext context)
        {
            _context = context;
        }

        public int Priority => 3;

        public async Task<bool> AppliesAsync(PriceCalculationContext context)
        {
            var hasCampaign = await _context.Campaigns
                .AnyAsync(c => c.IsActive &&
                              (c.CategoryIds.Contains(context.Product.CategoryId) ||
                               c.SellerIds.Contains(context.Product.SellerId)) &&
                              c.StartDate <= DateTime.UtcNow &&
                              c.EndDate >= DateTime.UtcNow);

            return hasCampaign;
        }

        public async Task<PriceAdjustment?> CalculateAdjustmentAsync(PriceCalculationContext context)
        {
            var campaign = await _context.Campaigns
                .Where(c => c.IsActive &&
                           (c.CategoryIds.Contains(context.Product.CategoryId) ||
                            c.SellerIds.Contains(context.Product.SellerId)) &&
                           c.StartDate <= DateTime.UtcNow &&
                           c.EndDate >= DateTime.UtcNow)
                .OrderByDescending(c => c.DiscountPercentage)
                .FirstOrDefaultAsync();

            if (campaign == null) return null;

            var discountAmount = context.CurrentPrice * (campaign.DiscountPercentage / 100m);

            return new PriceAdjustment
            {
                Type = PriceAdjustmentType.Campaign,
                Description = campaign.Name,
                Amount = discountAmount,
                Percentage = campaign.DiscountPercentage,
                Priority = Priority
            };
        }
    }
}
