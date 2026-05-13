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
        private readonly IEmailService _emailService;

        public AdminService(ArtesianDbContext context,
                            ISettingsService settingsService,
                            IHubContext<NotificationHub> hubContext,
                            IEmailService emailService)
        {
            _context = context;
            _settingsService = settingsService;
            _hubContext = hubContext;
            _emailService = emailService;
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .Where(u => !u.IsDeleted)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Role = u.Role,
                    ProfileImageUrl = u.ProfileImageUrl,
                    CPF = u.CPF,
                    Phone = u.Phone
                }).ToListAsync();

            return users;
        }

        public async Task DeleteUserAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.IsDeleted = true;
                await _context.SaveChangesAsync();
            }

        }

        public async Task<List<PendingSellerDto>> GetPendingSellersAsync()
        {
            return await _context.Sellers
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.IsApproved == false && (s.IsDeleted == false || s.IsDeleted == null))
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new PendingSellerDto
                {
                    Id = s.Id,
                    Name = s.StoreName ?? s.User.Name,
                    Email = s.User.Email,
                    Bio = s.Bio,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();
        }

        public async Task ApproveSellerAsync(Guid sellerId)
        {
            var seller = await _context.Sellers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == sellerId);

            if (seller == null) throw new KeyNotFoundException("Vendedor não encontrado.");

            seller.IsApproved = true;
            seller.StoreApproved = true; 
            seller.UpdatedAt = DateTime.UtcNow;

            if (seller.User != null)
            {
                seller.User.IsApproved = true;
                seller.StoreApproved = true;
                _context.Entry(seller.User).State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();

            if (seller.User != null)
            {
                await _emailService.SendApprovalEmailAsync(seller.User.Email, seller.User.Name);
            }

            await _hubContext.Clients.Group("Admins")
                .SendAsync("ReceiveNotification", new
                {
                    title = "Vendedor Aprovado!",
                    message = $"Vendedor {seller.StoreName} (de {seller.User?.Name}) agora pode vender na Trama.",
                    icon = "🎉"
                });
        }
        public async Task RejectSellerAsync(Guid sellerId)
        {
            var seller = await _context.Sellers.FindAsync(sellerId);
            if (seller == null) throw new KeyNotFoundException("Vendedor não encontrado.");

            seller.IsDeleted = true; 
            
            await _context.SaveChangesAsync();
        }

        public async Task<DashboardStatsResponse> GetDashboardStatsAsync()
        {
            var totalSales = await _context.Orders
                .Where(o => o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Sent || o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var ordersCount = await _context.Orders
                .CountAsync(o => o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Sent || o.Status == OrderStatus.Delivered);

            var newUsers = await _context.Users
                .CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30));

            var commissionRate = await _settingsService.GetCommissionRateAsync();
            var serviceFee = await _settingsService.GetServiceFeeAsync();

            var platformRevenue = (totalSales * commissionRate / 100) + (ordersCount * serviceFee);

            return new DashboardStatsResponse
            {
                TotalGMV = totalSales,
                TotalOrders = ordersCount,
                NewUsersLastMonth = newUsers,
                PlatformRevenue = platformRevenue,
                PendingApprovals = await _context.Sellers.CountAsync(s => !s.IsApproved && !s.IsDeleted)
            };
        }

        public async Task<List<CommissionReportItemResponse>> GetCommissionReportAsync()
        {
            var serviceFee = await _settingsService.GetServiceFeeAsync();
            var orders = await _context.Orders
                .Where(o =>
                    o.Status == OrderStatus.Confirmed ||
                    o.Status == OrderStatus.Sent ||
                    o.Status == OrderStatus.Delivered)
                .Include(o => o.Items.Where(i => i.SellerId != Guid.Empty))
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Seller)
                            .ThenInclude(s => s.User)
                .ToListAsync();

            var report = new Dictionary<Guid, CommissionReportItemResponse>();

            foreach (var order in orders)
            {
                var orderTotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
                if (orderTotal <= 0) continue;

                // Filter out items without a valid seller
                var validItems = order.Items.Where(i => i.Product?.Seller != null).ToList();
                if (!validItems.Any()) continue;

                var grouped = validItems.GroupBy(i => i.Product.SellerId);
                foreach (var group in grouped)
                {
                    var seller = group.First().Product.Seller;
                    var sellerSales = group.Sum(i => i.UnitPrice * i.Quantity);
                    var commissionRate = seller.CommissionRate;
                    var commissionEarned = sellerSales * (commissionRate / 100m);
                    var serviceFeeShare = serviceFee * (sellerSales / orderTotal);

                    if (!report.TryGetValue(seller.Id, out var item))
                    {
                        item = new CommissionReportItemResponse
                        {
                            SellerId = seller.Id,
                            SellerName = seller.StoreName ?? seller.User?.Name ?? "Seller",
                            TotalSales = 0m,
                            CommissionEarned = 0m,
                            Rate = commissionRate
                        };
                        report[seller.Id] = item;
                    }

                    item.TotalSales += sellerSales;
                    item.CommissionEarned += commissionEarned + serviceFeeShare;
                    item.Rate = commissionRate;
                }
            }

            return report.Values
                .OrderByDescending(x => x.TotalSales)
                .ToList();
        }

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

            if (rate.HasValue && (rate.Value < 0 || rate.Value > 100))
                throw new ArgumentException("Taxa deve ser entre 0 e 100");


            if (rate.HasValue)
            {
                seller.CommissionRate = rate.Value;
                await _context.SaveChangesAsync();
            }
        }

        public async Task UnbanCustomerAsync(Guid customerId)
        {
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted);

            if (customer == null)
                throw new KeyNotFoundException("Cliente não encontrado.");

            if (!customer.BannedAt.HasValue)
                throw new InvalidOperationException("Cliente não está banido.");

            customer.BannedAt = null;
            customer.UpdatedAt = DateTime.UtcNow;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
        }
    }
}
