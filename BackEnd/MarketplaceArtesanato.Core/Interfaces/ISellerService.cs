using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Core.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface ISellerService
    {
        Task<SellerResponseDto?> GetByIdAsync(Guid id);
        Task<SellerResponseDto?> GetByUserIdAsync(Guid userId);
        Task<List<SellerResponseDto>> SearchAsync(string term, int limit);
        Task<MomentResponseDto> CreateMomentAsync(Guid sellerId, CreateMomentDto dto);
        Task<List<MomentResponseDto>> GetMomentsAsync(Guid sellerId);
        Task<bool> IsOwnerAsync(Guid sellerId, Guid userId);
        Task<SellerDashboardDto> GetDashboardAsync(Guid sellerId);
        Task<List<SellerSaleResponseDto>> GetSalesAsync(Guid userId);
        Task<bool> UpdateProfileAsync(Guid userId, UpdateSellerProfileDto dto);
    }
}
