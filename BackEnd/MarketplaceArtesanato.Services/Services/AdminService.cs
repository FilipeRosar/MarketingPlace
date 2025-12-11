using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using MarketplaceArtesanato.Core.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace MarketplaceArtesanato.Services.Services
{
    public class AdminService : IAdminService
    {
        private readonly ArtesianDbContext _context;
        private readonly ISettingsService _settingsService;
        private readonly IHubContext<NotificationHub> _hubContext;
        public AdminService(ArtesianDbContext context,
                    ISettingsService settingsService,
                    IHubContext<NotificationHub> hubContext) 
        {
            _context = context;
            _settingsService = settingsService;
            _hubContext = hubContext;  
        }

        // --- GESTÃO DE USUÁRIOS ---
        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var customers = await _context.Customers
                .Where(c => !c.IsDeleted)
                .Select(c => new UserDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Email = c.Email,
                    Role = UserRole.Customer,
                    ProfileImageUrl = c.ProfileImageUrl,
                    CPF = c.CPF,
                    Phone = c.Phone
                }).ToListAsync();

            var sellers = await _context.Sellers
                .Where(s => !s.IsDeleted)
                .Select(s => new UserDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Email = s.Email,
                    Role = s.Role,
                    ProfileImageUrl = s.ProfileImageUrl,
                    CPF = s.CPF ?? string.Empty,
                    Phone = s.Phone
                }).ToListAsync();

            return customers.Concat(sellers).ToList();
        }

        public async Task DeleteUserAsync(Guid userId)
        {
            var customer = await _context.Customers.FindAsync(userId);
            if (customer != null)
            {
                customer.IsDeleted = true;
                await _context.SaveChangesAsync();
                return;
            }

            var seller = await _context.Sellers.FindAsync(userId);
            if (seller != null)
            {
                seller.IsDeleted = true;
                await _context.SaveChangesAsync();
            }
        }

        // --- APROVAÇÃO DE VENDEDORES ---
        public async Task<List<Seller>> GetPendingSellersAsync()
        {
            return await _context.Sellers
                .Where(s => !s.isAproved && !s.IsDeleted)
                .Include(s => s.Address)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task ApproveSellerAsync(Guid sellerId)
        {
            var seller = await _context.Sellers.FindAsync(sellerId);
            if (seller == null) throw new KeyNotFoundException("Vendedor não encontrado.");

            seller.isAproved = true;
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(seller.Id.ToString())
                .SendAsync("SellerApproved", new { message = "Seu cadastro de vendedor foi aprovado!" });
        }

        public async Task RejectSellerAsync(Guid sellerId)
        {
            var seller = await _context.Sellers.FindAsync(sellerId);
            if (seller == null) throw new KeyNotFoundException("Vendedor não encontrado.");

            seller.IsDeleted = true;
            await _context.SaveChangesAsync();
        }

        // --- DASHBOARD FINANCEIRO ---
        public async Task<DashboardStatsResponse> GetDashboardStatsAsync()
        {
            var totalSales = await _context.Orders
                .Where(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Sent || o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var ordersCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Paid || o.Status == OrderStatus.Sent || o.Status == OrderStatus.Delivered);

            var newUsers = await _context.Customers
                .CountAsync(c => c.CreatedAt >= DateTime.UtcNow.AddDays(-30));

            var newSellers = await _context.Sellers
                .CountAsync(s => s.CreatedAt >= DateTime.UtcNow.AddDays(-30));

            var commissionRate = await _settingsService.GetCommissionRateAsync();
            var serviceFee = await _settingsService.GetServiceFeeAsync();

            var platformRevenue = (totalSales * commissionRate / 100) + (ordersCount * serviceFee);

            return new DashboardStatsResponse
            {
                TotalGMV = totalSales,
                TotalOrders = ordersCount,
                NewUsersLastMonth = newUsers + newSellers,
                PlatformRevenue = platformRevenue,
                PendingApprovals = await _context.Sellers.CountAsync(s => !s.isAproved && !s.IsDeleted)
            };
        }

        // --- CONFIGURAÇÕES GLOBAIS ---
        public async Task UpdateCommissionRateAsync(decimal rate)
        {
            if (rate < 0 || rate > 100) throw new ArgumentException("Taxa deve ser entre 0 e 100");
            await _settingsService.UpdateSettingAsync("PlatformCommissionRate", rate.ToString("F2"));
        }

        public async Task UpdateServiceFeeAsync(decimal fee)
        {
            if (fee < 0) throw new ArgumentException("Taxa não pode ser negativa");
            await _settingsService.UpdateSettingAsync("ServiceFee", fee.ToString("F2"));
        }

        public async Task SetSellerCommissionAsync(Guid sellerId, decimal? rate)
        {
            var seller = await _context.Sellers.FindAsync(sellerId);
            if (seller == null) throw new KeyNotFoundException("Vendedor não encontrado.");

            if (rate.HasValue && (rate < 0 || rate > 100))
                throw new ArgumentException("Taxa deve ser entre 0 e 100");

            seller.CommissionRate = rate;
            await _context.SaveChangesAsync();
        }
    }
}