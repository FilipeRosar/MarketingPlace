using System;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IPlatformFeeService
    {
        Task<decimal> GetCommissionRateAsync(Guid sellerId, decimal additionalGross = 0m, DateTime? utcNow = null);
    }
}
