using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class CouponService : ICouponService
    {
        private readonly ArtesianDbContext _context;

        public CouponService(ArtesianDbContext context)
        {
            _context = context;
        }

        public async Task<CouponDto> CreateCouponAsync(CreateCouponDto dto)
        {
            // Validar datas
            if (dto.ValidUntil <= dto.ValidFrom)
                throw new InvalidOperationException("Data de validade deve ser após data inicial.");

            // Validar código único
            var existing = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == dto.Code);
            if (existing != null)
                throw new InvalidOperationException("Cupom com este código já existe.");

            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = dto.Code.ToUpper(),
                Description = dto.Description,
                Type = dto.Type,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MaxDiscount = dto.MaxDiscount,
                MinOrderValue = dto.MinOrderValue,
                Scope = dto.Scope,
                ProductId = dto.ProductId,
                CategoryId = dto.CategoryId,
                SellerId = dto.SellerId,
                PlatformSharePercentage = dto.PlatformSharePercentage,
                ValidFrom = dto.ValidFrom,
                ValidUntil = dto.ValidUntil,
                UsageLimit = dto.UsageLimit,
                UsageLimitPerUser = dto.UsageLimitPerUser,
                IsActive = dto.IsActive,
                PreventsCombination = dto.PreventsCombination,
                OnlyWithoutPromotion = dto.OnlyWithoutPromotion,
                OnlyFirstPurchase = dto.OnlyFirstPurchase,
                AutomationRuleId = dto.AutomationRuleId,
                CreatorSellerId = dto.CreatorSellerId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            return MapToDto(coupon);
        }

        public async Task<CouponDto?> GetCouponByIdAsync(Guid id)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            return coupon != null ? MapToDto(coupon) : null;
        }

        public async Task<CouponDto?> GetCouponByCodeAsync(string code)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code.ToUpper() && !c.IsDeleted);
            return coupon != null ? MapToDto(coupon) : null;
        }

        public async Task<List<CouponDto>> GetAllCouponsAsync(CouponType? type = null, bool? activeOnly = true)
        {
            var query = _context.Coupons.AsQueryable();

            if (!activeOnly.HasValue || activeOnly.Value)
                query = query.Where(c => c.IsActive);

            if (type.HasValue)
                query = query.Where(c => c.Type == type);

            query = query.Where(c => !c.IsDeleted);

            var couponsWithCreator = await query
                .GroupJoin(
                    _context.Users,
                    c => c.CreatorSellerId,
                    u => u.Id,
                    (c, creators) => new { Coupon = c, CreatorName = creators.FirstOrDefault().Name })
                .OrderByDescending(x => x.Coupon.CreatedAt)
                .ToListAsync();

            return couponsWithCreator.Select(x => MapToDto(x.Coupon, x.CreatorName)).ToList();
        }

        public async Task<List<CouponDto>> GetSellerCouponsAsync(Guid sellerId)
        {
            var couponsWithCreator = await _context.Coupons
                .Where(c => c.Type == CouponType.Seller && c.CreatorSellerId == sellerId && !c.IsDeleted)
                .GroupJoin(
                    _context.Users,
                    c => c.CreatorSellerId,
                    u => u.Id,
                    (c, creators) => new { Coupon = c, CreatorName = creators.FirstOrDefault().Name })
                .OrderByDescending(x => x.Coupon.CreatedAt)
                .ToListAsync();

            return couponsWithCreator.Select(x => MapToDto(x.Coupon, x.CreatorName)).ToList();
        }

        public async Task<CouponDto> UpdateCouponAsync(Guid id, UpdateCouponDto dto)
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (coupon == null)
                throw new KeyNotFoundException("Cupom não encontrado.");

            // Não permitir mudança de código
            if (dto.Description != null) coupon.Description = dto.Description;
            if (dto.DiscountValue.HasValue) coupon.DiscountValue = dto.DiscountValue.Value;
            if (dto.MaxDiscount.HasValue) coupon.MaxDiscount = dto.MaxDiscount;
            if (dto.MinOrderValue.HasValue) coupon.MinOrderValue = dto.MinOrderValue.Value;
            if (dto.ValidFrom.HasValue) coupon.ValidFrom = dto.ValidFrom.Value;
            if (dto.ValidUntil.HasValue) coupon.ValidUntil = dto.ValidUntil.Value;
            if (dto.UsageLimit.HasValue) coupon.UsageLimit = dto.UsageLimit.Value;
            if (dto.UsageLimitPerUser.HasValue) coupon.UsageLimitPerUser = dto.UsageLimitPerUser.Value;
            if (dto.IsActive.HasValue) coupon.IsActive = dto.IsActive.Value;
            if (dto.PreventsCombination.HasValue) coupon.PreventsCombination = dto.PreventsCombination.Value;
            if (dto.OnlyWithoutPromotion.HasValue) coupon.OnlyWithoutPromotion = dto.OnlyWithoutPromotion.Value;
            if (dto.OnlyFirstPurchase.HasValue) coupon.OnlyFirstPurchase = dto.OnlyFirstPurchase.Value;

            coupon.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToDto(coupon);
        }

        public async Task DeleteCouponAsync(Guid id)
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (coupon == null)
                throw new KeyNotFoundException("Cupom não encontrado.");

            coupon.IsDeleted = true;
            coupon.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task<CouponValidationResult> ValidateCouponAsync(
            string couponCode, 
            Guid userId, 
            decimal orderTotal, 
            List<Guid> productIds,
            Guid? sellerId = null)
        {
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == couponCode.ToUpper() && !c.IsDeleted);

            if (coupon == null)
                return InvalidResult("Cupom não encontrado.");

            if (!coupon.IsActive)
                return InvalidResult("Cupom inativo.");

            // Validar datas
            var now = DateTime.UtcNow;
            if (now < coupon.ValidFrom || now > coupon.ValidUntil)
                return InvalidResult("Cupom fora da validade.");

            // Validar valor mínimo
            if (orderTotal < coupon.MinOrderValue)
                return InvalidResult($"Compra mínima de {coupon.MinOrderValue:C} não atingida.");

            // Validar limite de uso total
            if (coupon.UsageLimit > 0 && coupon.UsageCount >= coupon.UsageLimit)
                return InvalidResult("Cupom atingiu limite de uso.");

            // Validar limite por usuário
            var userUsageCount = await _context.CouponUsages
                .CountAsync(cu => cu.CouponId == coupon.Id && cu.UserId == userId);

            if (userUsageCount >= coupon.UsageLimitPerUser)
                return InvalidResult("Limite de uso deste cupom por usuário atingido.");

            // Validar primeira compra se necessário
            if (coupon.OnlyFirstPurchase)
            {
                var userOrders = await _context.Orders
                    .Where(o => o.BuyerId == userId && o.Status != Core.Entities.Enums.OrderStatus.Canceled && o.Status != Core.Entities.Enums.OrderStatus.Refunded)
                    .CountAsync();

                if (userOrders > 0)
                    return InvalidResult("Este cupom é válido apenas na primeira compra.");
            }

            // Validar escopo
            if (!await ValidateCouponScope(coupon, productIds, sellerId))
                return InvalidResult("Cupom não é válido para os produtos selecionados.");

            // Calcular desconto
            var (discountAmount, paidBy, platformPays, sellerPays) = 
                CalculateDiscount(coupon, orderTotal, sellerId);

            return new CouponValidationResult
            {
                IsValid = true,
                Coupon = MapToDto(coupon),
                DiscountAmount = discountAmount,
                PaidBy = paidBy,
                PlatformPays = platformPays,
                SellerPays = sellerPays
            };
        }

        public async Task<CouponValidationResult> ApplyCouponAsync(
            Guid orderId, 
            string couponCode, 
            Guid userId, 
            decimal orderTotal, 
            List<Guid> productIds,
            Guid? sellerId = null)
        {
            // Validar primeiro
            var validation = await ValidateCouponAsync(couponCode, userId, orderTotal, productIds, sellerId);
            if (!validation.IsValid)
                return validation;

            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Code == couponCode.ToUpper());
            if (coupon == null)
                return InvalidResult("Cupom não encontrado.");

            // Registrar uso
            var usage = new CouponUsage
            {
                Id = Guid.NewGuid(),
                CouponId = coupon.Id,
                UserId = userId,
                OrderId = orderId,
                DiscountApplied = validation.DiscountAmount,
                PaidBy = validation.PaidBy,
                PlatformPaid = validation.PlatformPays,
                SellerPaid = validation.SellerPays,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            coupon.UsageCount++;
            _context.CouponUsages.Add(usage);
            await _context.SaveChangesAsync();

            return validation;
        }

        public async Task<List<CouponDto>> GetActiveCouponsAsync()
        {
            var now = DateTime.UtcNow;
            var couponsWithCreator = await _context.Coupons
                .Where(c => c.IsActive && c.ValidFrom <= now && c.ValidUntil > now && !c.IsDeleted)
                .GroupJoin(
                    _context.Users,
                    c => c.CreatorSellerId,
                    u => u.Id,
                    (c, creators) => new { Coupon = c, CreatorName = creators.FirstOrDefault().Name })
                .OrderByDescending(x => x.Coupon.CreatedAt)
                .ToListAsync();

            return couponsWithCreator.Select(x => MapToDto(x.Coupon, x.CreatorName)).ToList();
        }

        public async Task<List<CouponDto>> GetPlatformCouponsAsync()
        {
            var couponsWithCreator = await _context.Coupons
                .Where(c => c.Type == CouponType.Platform && !c.IsDeleted)
                .GroupJoin(
                    _context.Users,
                    c => c.CreatorSellerId,
                    u => u.Id,
                    (c, creators) => new { Coupon = c, CreatorName = creators.FirstOrDefault().Name })
                .OrderByDescending(x => x.Coupon.CreatedAt)
                .ToListAsync();

            return couponsWithCreator.Select(x => MapToDto(x.Coupon, x.CreatorName)).ToList();
        }

        public async Task<CouponUsageDto> GetCouponUsageAsync(Guid couponId)
        {
            var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == couponId && !c.IsDeleted);
            if (coupon == null)
                throw new KeyNotFoundException("Cupom não encontrado.");

            var usages = await _context.CouponUsages
                .Where(cu => cu.CouponId == couponId && !cu.IsDeleted)
                .Include(cu => cu.User)
                .OrderByDescending(cu => cu.CreatedAt)
                .Take(10)
                .ToListAsync();

            return new CouponUsageDto
            {
                TotalUses = coupon.UsageCount,
                RemainingUses = coupon.UsageLimit > 0 ? coupon.UsageLimit - coupon.UsageCount : -1,
                TotalDiscountGiven = usages.Sum(u => u.DiscountApplied),
                RecentUses = usages.Select(u => new CouponUseDetailDto
                {
                    UserId = u.UserId,
                    UserName = u.User.Name,
                    OrderId = u.OrderId,
                    DiscountApplied = u.DiscountApplied,
                    UsedAt = u.CreatedAt
                }).ToList()
            };
        }

        // ========================
        // Métodos privados
        // ========================

        private async Task<bool> ValidateCouponScope(Coupon coupon, List<Guid> productIds, Guid? sellerId)
        {
            switch (coupon.Scope)
            {
                case CouponScope.EntireOrder:
                    return true;

                case CouponScope.Product:
                    if (coupon.ProductId == null || !productIds.Contains(coupon.ProductId.Value))
                        return false;
                    break;

                case CouponScope.Category:
                    if (coupon.CategoryId == null)
                        return false;

                    // Verificar se algum produto está na categoria
                    var productInCategory = await _context.Products
                        .AnyAsync(p => productIds.Contains(p.Id) && p.CategoryId == coupon.CategoryId);

                    if (!productInCategory)
                        return false;
                    break;

                case CouponScope.Seller:
                    if (coupon.SellerId == null)
                        return false;

                    // Verificar se algum produto é do seller
                    var productFromSeller = await _context.Products
                        .AnyAsync(p => productIds.Contains(p.Id) && p.SellerId == coupon.SellerId);

                    if (!productFromSeller)
                        return false;
                    break;

                case CouponScope.WithoutPromotion:
                    // Verificar se produtos tem promoção ativa
                    var productsWithPromo = await _context.Products
                        .Where(p => productIds.Contains(p.Id) && p.SalePrice.HasValue)
                        .AnyAsync();

                    if (productsWithPromo)
                        return false;
                    break;
            }

            return true;
        }

        private (decimal discountAmount, DiscountPaidBy paidBy, decimal? platformPays, decimal? sellerPays) 
            CalculateDiscount(Coupon coupon, decimal orderTotal, Guid? sellerId)
        {
            decimal discountAmount = 0;

            if (coupon.DiscountType == DiscountType.Percentage)
            {
                discountAmount = orderTotal * (coupon.DiscountValue / 100);
                if (coupon.MaxDiscount.HasValue && discountAmount > coupon.MaxDiscount)
                    discountAmount = coupon.MaxDiscount.Value;
            }
            else
            {
                discountAmount = coupon.DiscountValue;
                if (discountAmount > orderTotal)
                    discountAmount = orderTotal;
            }

            // Determinar quem paga
            DiscountPaidBy paidBy = coupon.Type switch
            {
                CouponType.Platform => DiscountPaidBy.Platform,
                CouponType.Seller => DiscountPaidBy.Seller,
                _ => DiscountPaidBy.Platform
            };

            decimal? platformPays = null;
            decimal? sellerPays = null;

            if (coupon.PlatformSharePercentage.HasValue)
            {
                paidBy = DiscountPaidBy.Hybrid;
                platformPays = discountAmount * (coupon.PlatformSharePercentage.Value / 100m);
                sellerPays = discountAmount - platformPays;
            }
            else if (coupon.Type == CouponType.Platform)
            {
                platformPays = discountAmount;
            }
            else if (coupon.Type == CouponType.Seller)
            {
                sellerPays = discountAmount;
            }

            return (discountAmount, paidBy, platformPays, sellerPays);
        }

        private CouponValidationResult InvalidResult(string errorMessage)
        {
            return new CouponValidationResult
            {
                IsValid = false,
                ErrorMessage = errorMessage
            };
        }

        private CouponDto MapToDto(Coupon coupon)
        {
            return new CouponDto
            {
                Id = coupon.Id,
                Code = coupon.Code,
                Description = coupon.Description,
                Type = coupon.Type,
                DiscountType = coupon.DiscountType,
                DiscountValue = coupon.DiscountValue,
                MaxDiscount = coupon.MaxDiscount,
                MinOrderValue = coupon.MinOrderValue,
                Scope = coupon.Scope,
                ProductId = coupon.ProductId,
                CategoryId = coupon.CategoryId,
                SellerId = coupon.SellerId,
                PlatformSharePercentage = coupon.PlatformSharePercentage,
                ValidFrom = coupon.ValidFrom,
                ValidUntil = coupon.ValidUntil,
                UsageLimit = coupon.UsageLimit,
                UsageCount = coupon.UsageCount,
                UsageLimitPerUser = coupon.UsageLimitPerUser,
                IsActive = coupon.IsActive,
                PreventsCombination = coupon.PreventsCombination,
                OnlyWithoutPromotion = coupon.OnlyWithoutPromotion,
                OnlyFirstPurchase = coupon.OnlyFirstPurchase,
                CreatedAt = coupon.CreatedAt,
                UpdatedAt = coupon.UpdatedAt,
                CreatorSellerId = coupon.CreatorSellerId
            };
        }

        private CouponDto MapToDto(Coupon coupon, string? creatorSellerName = null)
        {
            return new CouponDto
            {
                Id = coupon.Id,
                Code = coupon.Code,
                Description = coupon.Description,
                Type = coupon.Type,
                DiscountType = coupon.DiscountType,
                DiscountValue = coupon.DiscountValue,
                MaxDiscount = coupon.MaxDiscount,
                MinOrderValue = coupon.MinOrderValue,
                Scope = coupon.Scope,
                ProductId = coupon.ProductId,
                CategoryId = coupon.CategoryId,
                SellerId = coupon.SellerId,
                PlatformSharePercentage = coupon.PlatformSharePercentage,
                ValidFrom = coupon.ValidFrom,
                ValidUntil = coupon.ValidUntil,
                UsageLimit = coupon.UsageLimit,
                UsageCount = coupon.UsageCount,
                UsageLimitPerUser = coupon.UsageLimitPerUser,
                IsActive = coupon.IsActive,
                PreventsCombination = coupon.PreventsCombination,
                OnlyWithoutPromotion = coupon.OnlyWithoutPromotion,
                OnlyFirstPurchase = coupon.OnlyFirstPurchase,
                CreatedAt = coupon.CreatedAt,
                UpdatedAt = coupon.UpdatedAt,
                CreatorSellerId = coupon.CreatorSellerId,
                CreatorSellerName = creatorSellerName
            };
        }
    }
}
