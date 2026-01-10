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
        Task<AuthResponseDto> RegisterCustomerAsync(RegisterCostumerDto dto);
        Task<AuthResponseDto> RegisterSellerAsync(RegisterSellerDto dto);
        Task<AuthResponseDto> LoginAsync(LoginDto dto);
        Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordDto dto);
        Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordDto dto);
        Task<AuthResponseDto> GetCurrentUserAsync();
    }
}
