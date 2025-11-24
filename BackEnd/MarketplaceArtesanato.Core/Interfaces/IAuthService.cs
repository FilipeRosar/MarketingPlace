using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.Core.Entities.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> LoginAsync(LoginDto request);
        Task<AuthResponseDto> RegisterCustomerAsync(RegisterCostumerDto request);
        Task<AuthResponseDto> RegisterSellerAsync(RegisterSellerDto request);
    }
}
