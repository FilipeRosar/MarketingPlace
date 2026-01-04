using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IAdminService
    {
        Task<List<UserDto>> GetAllUsersAsync();
        Task DeleteUserAsync(Guid userId);
        Task<List<PendingSellerDto>> GetPendingSellersAsync();
        Task ApproveSellerAsync(Guid sellerId);
        Task RejectSellerAsync(Guid sellerId);
        Task<DashboardStatsResponse> GetDashboardStatsAsync();
        Task UpdateCommissionRateAsync(decimal rate);
        Task UpdateServiceFeeAsync(decimal fee);
        Task SetSellerCommissionAsync(Guid sellerId, decimal? rate);
    }
}
