using MarketplaceArtesanato.Core.Models.Requests;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IUserService
    {
        Task<bool> UpdateProfileImageAsync(Guid userId, string role, string imageUrl);
        Task<bool> UpdateProfileAsync(Guid userId, string role, UpdateUserDto dto);
        Task<bool> DeleteAccountAsync(Guid userId, string password);
    }
}