using MarketplaceArtesanato.Core.Entities.Models.Responses;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IStripeConnectService
    {
        Task<string> CreateOnboardingLinkAsync(Guid userId);
        Task<string> CreateDashboardLinkAsync(Guid userId);
        Task<StripeConnectStatusDto> GetStatusAsync(Guid userId);
    }
}
