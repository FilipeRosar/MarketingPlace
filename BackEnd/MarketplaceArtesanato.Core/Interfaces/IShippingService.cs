using MarketplaceArtesanato.Core.Entities.Models.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IShippingService
    {
        Task<List<ShippingOptionDto>> CalculateShippingAsync(CalculateShippingRequest request);
        Task<string> GenerateLabelAsync(GenerateLabelRequest request);
    }
}
