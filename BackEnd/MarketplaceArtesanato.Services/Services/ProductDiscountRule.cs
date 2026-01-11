using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class ProductDiscountRule : IPriceRule
    {
        public int Priority => 1;

        public Task<bool> AppliesAsync(PriceCalculationContext context)
        {
            return Task.FromResult(
                context.Product.HasDiscount &&
                context.Product.OriginalPrice.HasValue &&
                context.Product.OriginalPrice.Value > context.Product.Price
            );
        }

        public Task<PriceAdjustment?> CalculateAdjustmentAsync(PriceCalculationContext context)
        {
            var discount = context.Product.OriginalPrice!.Value - context.Product.Price;

            return Task.FromResult<PriceAdjustment?>(new PriceAdjustment
            {
                Type = PriceAdjustmentType.ProductDiscount,
                Description = "Desconto do produto",
                Amount = discount,
                Percentage = (discount / context.Product.OriginalPrice.Value) * 100,
                Priority = Priority
            });
        }
    }

}
