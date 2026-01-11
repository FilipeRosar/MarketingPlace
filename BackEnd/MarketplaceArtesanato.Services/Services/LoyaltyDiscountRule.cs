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
    public class LoyaltyDiscountRule : IPriceRule
    {
        private readonly ArtesianDbContext _context;

        public LoyaltyDiscountRule(ArtesianDbContext context)
        {
            _context = context;
        }

        public int Priority => 5;

        public async Task<bool> AppliesAsync(PriceCalculationContext context)
        {
            if (!context.UserId.HasValue) return false;

            var orderCount = await _context.Orders
                .CountAsync(o => o.BuyerId == context.UserId.Value &&
                                o.Status == Core.Entities.Enums.OrderStatus.Delivered);

            return orderCount >= 5; 
        }

        public async Task<PriceAdjustment?> CalculateAdjustmentAsync(PriceCalculationContext context)
        {
            if (!context.UserId.HasValue) return null;

            var orderCount = await _context.Orders
                .CountAsync(o => o.BuyerId == context.UserId.Value &&
                                o.Status == Core.Entities.Enums.OrderStatus.Delivered);

            // 2% para cada 5 pedidos, máximo 10%
            var discountPercentage = Math.Min((orderCount / 5) * 2, 10);
            var discountAmount = context.CurrentPrice * (discountPercentage / 100m);

            return new PriceAdjustment
            {
                Type = PriceAdjustmentType.LoyaltyDiscount,
                Description = $"Desconto fidelidade ({orderCount} pedidos)",
                Amount = discountAmount,
                Percentage = discountPercentage,
                Priority = Priority
            };
        }
    }
}
