using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    /// <summary>
    /// Implementação do serviço de cálculo de preços
    /// Usa padrão Chain of Responsibility para aplicar múltiplas regras de preço
    /// </summary>
    public class PriceCalculationService : IPriceCalculationService
    {
        private readonly ArtesianDbContext _context;
        private readonly ILogger<PriceCalculationService> _logger;
        private readonly List<IPriceRule> _priceRules;

        public PriceCalculationService(
            ArtesianDbContext context,
            ILogger<PriceCalculationService> logger,
            IEnumerable<IPriceRule> priceRules)
        {
            _context = context;
            _logger = logger;
            _priceRules = priceRules.OrderBy(r => r.Priority).ToList();
        }

        public async Task<ProductPriceResult> CalculateProductPriceAsync(
            Product product,
            Guid? userId = null,
            string? couponCode = null)
        {
            var result = new ProductPriceResult
            {
                ProductId = product.Id,
                BasePrice = product.Price,
                FinalPrice = product.Price
            };

            var context = new PriceCalculationContext
            {
                Product = product,
                UserId = userId,
                CouponCode = couponCode,
                CurrentPrice = product.Price
            };

            // Aplica cada regra de preço em ordem de prioridade
            foreach (var rule in _priceRules)
            {
                try
                {
                    if (await rule.AppliesAsync(context))
                    {
                        var adjustment = await rule.CalculateAdjustmentAsync(context);
                        if (adjustment != null && adjustment.Amount != 0)
                        {
                            result.Adjustments.Add(adjustment);
                            context.CurrentPrice -= adjustment.Amount;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao aplicar regra de preço {RuleType} para produto {ProductId}",
                        rule.GetType().Name, product.Id);
                }
            }

            result.FinalPrice = Math.Max(0, context.CurrentPrice);
            result.TotalDiscount = result.BasePrice - result.FinalPrice;

            return result;
        }

        public async Task<Dictionary<Guid, ProductPriceResult>> CalculateBulkPricesAsync(
            IEnumerable<Product> products,
            Guid? userId = null,
            string? couponCode = null)
        {
            var results = new Dictionary<Guid, ProductPriceResult>();

            // Calcula preço de cada produto
            foreach (var product in products)
            {
                var priceResult = await CalculateProductPriceAsync(product, userId, couponCode);
                results[product.Id] = priceResult;
            }

            return results;
        }

        public async Task<CartPriceResult> CalculateCartPriceAsync(
            IEnumerable<CartItemDto> items,
            Guid userId,
            string? couponCode = null)
        {
            var result = new CartPriceResult();

            // Carrega produtos do banco
            var productIds = items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            // Calcula preço de cada item
            foreach (var item in items)
            {
                if (products.TryGetValue(item.ProductId, out var product))
                {
                    var priceResult = await CalculateProductPriceAsync(product, userId, couponCode);
                    result.ItemPrices[item.ProductId] = priceResult;
                    result.Subtotal += priceResult.FinalPrice * item.Quantity;
                }
            }

            // Aplica descontos de carrinho (cupons, etc)
            if (!string.IsNullOrEmpty(couponCode))
            {
                var cartDiscount = await ApplyCartCouponAsync(couponCode, result.Subtotal, userId);
                if (cartDiscount != null)
                {
                    result.CartLevelAdjustments.Add(cartDiscount);
                    result.TotalDiscount += cartDiscount.Amount;
                }
            }

            result.FinalTotal = Math.Max(0, result.Subtotal - result.TotalDiscount + result.ShippingCost);

            return result;
        }

        private async Task<PriceAdjustment?> ApplyCartCouponAsync(string couponCode, decimal subtotal, Guid userId)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == couponCode &&
                                         c.IsActive &&
                                         c.ExpiresAt > DateTime.UtcNow);

            if (coupon == null) return null;

            // Verifica se o usuário já usou o cupom (se for de uso único)
            if (coupon.MaxUsesPerUser.HasValue)
            {
                var usageCount = await _context.CouponUsages
                    .CountAsync(u => u.CouponId == coupon.Id && u.UserId == userId);

                if (usageCount >= coupon.MaxUsesPerUser.Value)
                    return null;
            }

            decimal discountAmount = coupon.DiscountType == "Percentage"
                ? subtotal * (coupon.DiscountValue / 100m)
                : coupon.DiscountValue;

            if (coupon.MaxDiscountAmount.HasValue)
            {
                discountAmount = Math.Min(discountAmount, coupon.MaxDiscountAmount.Value);
            }

            return new PriceAdjustment
            {
                Type = PriceAdjustmentType.Coupon,
                Description = $"Cupom: {coupon.Code}",
                Amount = discountAmount,
                Percentage = coupon.DiscountType == "Percentage" ? coupon.DiscountValue : 0,
                Priority = 100
            };
        }
    }
}
   
