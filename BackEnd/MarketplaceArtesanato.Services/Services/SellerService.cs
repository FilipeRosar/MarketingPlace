using AutoMapper;
using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceArtesanato.Services.Services
{
    public class SellerService : ISellerService
    {
        private readonly ArtesianDbContext _context;
        private readonly IMapper _mapper;
        private const string AccentInsensitiveCollation = "Latin1_General_CI_AI";

        public SellerService(ArtesianDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<SellerResponseDto?> GetByIdAsync(Guid id)
        {
            var seller = await _context.Sellers
                .Include(s => s.Address)
                .Include(s => s.Moments) 
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

        public async Task<List<SellerResponseDto>> SearchAsync(string term, int limit)
        {
            if (string.IsNullOrWhiteSpace(term))
                return new List<SellerResponseDto>();

            var normalized = term.Trim();

            var sellers = await _context.Sellers
                .AsNoTracking()
                .Include(s => s.Address)
                .Where(s => s.IsApproved)
                .Where(s =>
                    EF.Functions.Like(EF.Functions.Collate(s.StoreName, AccentInsensitiveCollation), $"%{normalized}%") ||
                    (s.Bio != null && EF.Functions.Like(EF.Functions.Collate(s.Bio, AccentInsensitiveCollation), $"%{normalized}%")))
                .OrderByDescending(s => s.RatingAverage)
                .ThenByDescending(s => s.TotalSales)
                .Take(limit)
                .ToListAsync();

            return _mapper.Map<List<SellerResponseDto>>(sellers);
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
                    (i.Order.Status == OrderStatus.Confirmed ||
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
                SellerId = sellerId, 
                TotalRevenue = totalRevenue,
                TotalSales = totalSales,
                ActiveProducts = activeProducts,
                DailyRevenue = dailyRevenue
            };
        }

        public async Task<List<SellerSaleResponseDto>> GetSalesAsync(Guid userId)
        {
            var seller = await _context.Sellers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                return new List<SellerSaleResponseDto>();

            var sellerId = seller.Id;

            var sales = await _context.Orders
                .AsNoTracking()
                .Include(o => o.Buyer)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Where(o => o.Items.Any(i => i.Product.SellerId == sellerId))
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new SellerSaleResponseDto
                {
                    OrderId = o.Id,
                    DisplayOrderId = "#" + o.Id.ToString("N").Substring(0, 8).ToUpper(),
                    CustomerName = o.Buyer != null ? o.Buyer.Name ?? "Cliente Trama" : "Cliente Trama",
                    OrderDate = o.CreatedAt,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status.ToString(),
                    TrackingCode = o.TrackingCodes != null && o.TrackingCodes.ContainsKey(sellerId)
                        ? o.TrackingCodes[sellerId]
                        : null,
                    Carrier = o.Carrier,
                    Items = o.Items.Select(i => new SellerSaleItemDto
                    {
                        ProductName = i.Product.Name,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        Total = i.UnitPrice * i.Quantity
                    }).ToList()
                })
                .ToListAsync();

            return sales;
        }

        public async Task<bool> UpdateProfileAsync(Guid userId, UpdateSellerProfileDto dto)
        {
            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (seller == null)
                return false;

            seller.StoreName = dto.Name.Trim();
            seller.Bio = dto.Bio;
            seller.InstagramUrl = dto.Instagram;
            seller.FacebookUrl = dto.Facebook;
            seller.TiktokUrl = dto.Tiktok;
            seller.YoutubeUrl = dto.Youtube;
            seller.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
