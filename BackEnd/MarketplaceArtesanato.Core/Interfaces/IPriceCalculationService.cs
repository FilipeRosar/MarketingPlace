using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface IPriceCalculationService
    {
        Task<ProductPriceResult> CalculateProductPriceAsync(
        Product product,
        Guid? userId = null,
        string? couponCode = null);

        // Calcula preços de MÚLTIPLOS produtos (otimizado)
        Task<Dictionary<Guid, ProductPriceResult>> CalculateBulkPricesAsync(
            IEnumerable<Product> products,
            Guid? userId = null,
            string? couponCode = null);

        // Calcula total do CARRINHO com cupons
        Task<CartPriceResult> CalculateCartPriceAsync(
            IEnumerable<CartItemDto> items,
            Guid userId,
            string? couponCode = null);
    }
}
