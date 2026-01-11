using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Data.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IPriceRule
    {
        int Priority { get; }
        Task<bool> AppliesAsync(PriceCalculationContext context);
        Task<PriceAdjustment?> CalculateAdjustmentAsync(PriceCalculationContext context);
    }
}
