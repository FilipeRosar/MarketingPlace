using AutoMapper;
using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceArtesanato.Services.Services
{
    public class SellerService : ISellerService
    {
        private readonly ArtesianDbContext _context;
        private readonly IMapper _mapper;

        public SellerService(ArtesianDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<SellerResponseDto?> GetByIdAsync(Guid id)
        {
            var seller = await _context.Sellers
                .Include(s => s.Address)
                .Include(s => s.Moments) // Inclui os momentos
                .FirstOrDefaultAsync(s => s.Id == id);

            return seller == null ? null : _mapper.Map<SellerResponseDto>(seller);
        }

        public async Task<SellerResponseDto?> GetByUserIdAsync(Guid userId)
        {
            var seller = await _context.Sellers
                .Include(s => s.Address)
                .Include(s => s.Moments)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            return seller == null ? null : _mapper.Map<SellerResponseDto>(seller);
        }

        public async Task<MomentResponseDto> CreateMomentAsync(Guid sellerId, CreateMomentDto dto)
        {
            var moment = _mapper.Map<Moment>(dto);
            moment.SellerId = sellerId;
            moment.CreatedAt = DateTime.UtcNow;

            _context.Moments.Add(moment);
            await _context.SaveChangesAsync();

            return _mapper.Map<MomentResponseDto>(moment);
        }

        public async Task<List<MomentResponseDto>> GetMomentsAsync(Guid sellerId)
        {
            var moments = await _context.Moments
                .Where(m => m.SellerId == sellerId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return _mapper.Map<List<MomentResponseDto>>(moments);
        }
        public async Task<bool> IsOwnerAsync(Guid sellerId, Guid userId)
        {
            return await _context.Sellers
                .AnyAsync(s => s.Id == sellerId && s.UserId == userId);
        }

        public async Task<SellerDashboardDto?> GetDashboardAsync(Guid userId)
        {
            var seller = await _context.Sellers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                return null;

            var sellerId = seller.Id;
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-6);

            var salesData = await _context.OrderItems
                .AsNoTracking()
                .Where(i =>
                    i.Product.SellerId == sellerId &&
                    i.Order.CreatedAt.Date >= startDate &&
                    (i.Order.Status == OrderStatus.Paid ||
                     i.Order.Status == OrderStatus.Sent ||
                     i.Order.Status == OrderStatus.Delivered))
                .Select(i => new
                {
                    Date = i.Order.CreatedAt.Date,
                    Total = i.UnitPrice * i.Quantity,
                    i.OrderId
                })
                .ToListAsync();

            var totalRevenue = salesData.Sum(x => x.Total);
            var totalSales = salesData.Select(x => x.OrderId).Distinct().Count();

            var dailyRevenue = Enumerable.Range(0, 7)
                .Select(offset =>
                {
                    var day = startDate.AddDays(offset);
                    return new DailyRevenueDto
                    {
                        Date = day,
                        Revenue = salesData
                            .Where(x => x.Date == day)
                            .Sum(x => x.Total)
                    };
                })
                .ToList();

            var activeProducts = await _context.Products
                .CountAsync(p => p.SellerId == sellerId && !p.IsDeleted);

            return new SellerDashboardDto
            {
                SellerId = sellerId, // 🔥 FUNDAMENTAL
                TotalRevenue = totalRevenue,
                TotalSales = totalSales,
                ActiveProducts = activeProducts,
                DailyRevenue = dailyRevenue
            };
        }

    }
}