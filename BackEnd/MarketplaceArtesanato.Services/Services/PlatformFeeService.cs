using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceArtesanato.Services.Services
{
    public class PlatformFeeService : IPlatformFeeService
    {
        private readonly ArtesianDbContext _context;

        public PlatformFeeService(ArtesianDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetCommissionRateAsync(Guid sellerId, decimal additionalGross = 0m, DateTime? utcNow = null)
        {
            if (sellerId == Guid.Empty)
            {
                return 15m;
            }

            var now = utcNow ?? DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            var monthlyGross = await _context.OrderItems
                .AsNoTracking()
                .Where(i => i.Product.SellerId == sellerId
                    && i.Order.CreatedAt >= monthStart
                    && i.Order.CreatedAt < monthEnd
                    && (i.Order.Status == OrderStatus.Confirmed
                        || i.Order.Status == OrderStatus.Processing
                        || i.Order.Status == OrderStatus.Sent
                        || i.Order.Status == OrderStatus.Delivered))
                .SumAsync(i => (decimal?)(i.UnitPrice * i.Quantity)) ?? 0m;

            var projectedGross = monthlyGross + (additionalGross < 0m ? 0m : additionalGross);

            if (projectedGross <= 3000m) return 15m;
            if (projectedGross <= 10000m) return 12m;
            return 10m;
        }
    }
}
