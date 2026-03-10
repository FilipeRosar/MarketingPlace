using AutoMapper;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class ProductService : IProductService
    {
        private readonly ArtesianDbContext _context;
        private readonly IMapper _mapper;
        private readonly IStorageService _storage;
        private const string AccentInsensitiveCollation = "Latin1_General_CI_AI";

        public ProductService(ArtesianDbContext context, IMapper mapper, IStorageService storage)
        {
            _context = context;
            _mapper = mapper;
            _storage = storage;
        }

        public async Task<PaginatedResult<ProductResponseDto>> GetAllAsync(int page, int pageSize, string? search, string? subcategory, int? category, decimal? minPrice, decimal? maxPrice, Guid? sellerId)
        {
            var query = _context.Products
                .Include(p => p.Seller!).ThenInclude(s => s.Address)
                .Include(p => p.Seller!).ThenInclude(s => s.Subscription)
                .Include(p => p.Ratings!).ThenInclude(r => r.Customer)
                .Include(p => p.Images)
                .Include(p => p.StoryMedia)
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            // Filtros
            if (sellerId.HasValue)
                query = query.Where(p => p.SellerId == sellerId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(p =>
                    EF.Functions.Like(EF.Functions.Collate(p.Name, AccentInsensitiveCollation), $"%{term}%") ||
                    (p.Description != null && EF.Functions.Like(EF.Functions.Collate(p.Description, AccentInsensitiveCollation), $"%{term}%")) ||
                    EF.Functions.Like(EF.Functions.Collate(p.Seller!.StoreName, AccentInsensitiveCollation), $"%{term}%") ||
                    (!string.IsNullOrEmpty(p.Tags) && EF.Functions.Like(EF.Functions.Collate(p.Tags, AccentInsensitiveCollation), $"%{term}%")));
            }

            if (!string.IsNullOrWhiteSpace(subcategory))
            {
                var sub = subcategory.Trim();
                query = query.Where(p =>
                    !string.IsNullOrEmpty(p.Tags) &&
                    EF.Functions.Like(EF.Functions.Collate(p.Tags, AccentInsensitiveCollation), $"%{sub}%"));
            }

            if (category.HasValue)
                query = query.Where(p => (int)p.Category == category.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // Paginação
            var total = await query.CountAsync();
            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<ProductResponseDto>>(products);

            await ApplyActivePromotionsAsync(dtos);

            return new PaginatedResult<ProductResponseDto>
            {
                Data = dtos,
                Total = total,
                Page = page,
                PageSize = pageSize,
                Pages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public async Task<ProductResponseDto?> GetByIdAsync(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Seller!).ThenInclude(s => s.Address)
                .Include(p => p.Seller!).ThenInclude(s => s.Subscription)
                .Include(p => p.Ratings!).ThenInclude(r => r.Customer)
                .Include(p => p.Images)
                .Include(p => p.StoryMedia)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return null;

            var dto = _mapper.Map<ProductResponseDto>(product);

            await ApplyActivePromotionsAsync(new List<ProductResponseDto> { dto });

            return dto;
        }

        public async Task<ProductResponseDto> CreateAsync(Guid sellerId, CreateProductDto dto)
        {
            var seller = await _context.Sellers
                .Include(s => s.Subscription)
                .FirstOrDefaultAsync(s => s.UserId == sellerId);
            if (seller == null) throw new KeyNotFoundException("Vendedor não encontrado.");

            if (!seller.IsApproved)
                throw new UnauthorizedAccessException("Sua loja ainda está em análise. Aguarde a aprovação para publicar produtos.");

            // Validar acesso à feature de história
            if (dto.StoryEnabled)
            {
                var hasSubscription = seller.Subscription != null && seller.Subscription.IsActive;
                var isProOrPremium = hasSubscription && (seller.Subscription.Plan == SellerPlan.Pro || seller.Subscription.Plan == SellerPlan.Premium);
                
                if (!isProOrPremium)
                {
                    throw new UnauthorizedAccessException("Apenas vendedores com plano Pro ou Premium podem criar histórias de produtos. Atualize seu plano para desbloquear essa funcionalidade.");
                }
            }

            var storyMediaUrls = new List<string>();
            if (dto.StoryEnabled && dto.StoryMedia != null)
            {
                foreach (var file in dto.StoryMedia)
                {
                    if (file.Length > 0 && IsImage(file))
                    {
                        var url = await _storage.UploadFileAsync(file);
                        storyMediaUrls.Add(url);
                    }
                }
            }

            // Upload de Imagens
            var imageUrls = new List<string>();
            if (dto.Images != null)
            {
                foreach (var file in dto.Images)
                {
                    if (file.Length > 0 && IsImage(file))
                    {
                        var url = await _storage.UploadFileAsync(file);
                        imageUrls.Add(url);
                    }
                }
            }

            var product = _mapper.Map<Product>(dto);
            product.Id = Guid.NewGuid();
            product.SellerId = seller.Id;
            // product.Seller = seller; // Redundant if SellerId is set, but harmless.

            product.Images = imageUrls.Select(url => new ProductImage
            {
                Id = Guid.NewGuid(),
                Url = url,
                ProductId = product.Id,
                IsMain = imageUrls.First() == url
            }).ToList();

            if (!dto.StoryEnabled)
            {
                product.StoryEnabled = false;
                product.StoryMaker = null;
                product.StoryExperience = null;
                product.StoryInspiration = null;
                product.StoryMarkdown = null;
                product.StoryMedia = new List<ProductStoryMedia>();
            }
            else
            {
                product.StoryEnabled = true;
                product.StoryMedia = storyMediaUrls.Select(url => new ProductStoryMedia
                {
                    Id = Guid.NewGuid(),
                    Url = url,
                    ProductId = product.Id
                }).ToList();
            }

            product.CreatedAt = DateTime.UtcNow;
            product.IsDeleted = false;

            if (dto.SalePrice.HasValue && dto.SalePrice > 0)
            {
                product.SalePrice = dto.SalePrice.Value;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // To return the full DTO with Seller info, we might need to load it explicitly or map carefully
            // Since 'product.Seller' might be null in the returned entity if not explicitly set or loaded.
            product.Seller = seller;

            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task<bool> UpdateAsync(Guid id, Guid userId, string userRole, UpdateProductDto dto)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return false; // Not Found

            // Verifica permissao: sellerId do produto vs sellerId do usuario
            if (userRole != "Admin")
            {
                var seller = await _context.Sellers
                    .Include(s => s.Subscription)
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (seller == null || product.SellerId != seller.Id)
                    throw new UnauthorizedAccessException("Sem permissao para editar este produto.");

                // Validar acesso à feature de história
                if (dto.StoryEnabled.HasValue && dto.StoryEnabled.Value && !product.StoryEnabled)
                {
                    var hasSubscription = seller.Subscription != null && seller.Subscription.IsActive;
                    var isProOrPremium = hasSubscription && (seller.Subscription.Plan == SellerPlan.Pro || seller.Subscription.Plan == SellerPlan.Premium);
                    
                    if (!isProOrPremium)
                    {
                        throw new UnauthorizedAccessException("Apenas vendedores com plano Pro ou Premium podem criar histórias de produtos. Atualize seu plano para desbloquear essa funcionalidade.");
                    }
                }
            }

            _mapper.Map(dto, product);
            product.UpdatedAt = DateTime.UtcNow;
            if (dto.BoostProduct == true)
            {
                var now = DateTime.UtcNow;
                if (product.BoostedUntil.HasValue && product.BoostedUntil.Value > now)
                {
                    throw new InvalidOperationException("Boost disponivel apenas apos o periodo atual.");
                }

                var subscription = await _context.SellerSubscriptions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SellerId == product.SellerId && s.IsActive);

                if (subscription == null || !subscription.CanHighlightProducts || subscription.HighlightLimit <= 0)
                {
                    throw new InvalidOperationException("Boost disponivel apenas para planos Pro ou Premium.");
                }

                var activeBoosts = await _context.Products
                    .AsNoTracking()
                    .CountAsync(p => p.SellerId == product.SellerId &&
                                     p.BoostedUntil.HasValue &&
                                     p.BoostedUntil.Value > now);

                if (activeBoosts >= subscription.HighlightLimit)
                {
                    throw new InvalidOperationException("Limite de boosts do plano atingido.");
                }

                product.BoostedUntil = now.AddMonths(1);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(Guid id, Guid userId, string userRole)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.StoryMedia)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return false;

            if (userRole != "Admin")
            {
                var seller = await _context.Sellers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (seller == null || product.SellerId != seller.Id)
                    throw new UnauthorizedAccessException("Sem permissao para deletar este produto.");
            }

            // Remove imagens do storage
            foreach (var image in product.Images)
            {
                try { await _storage.DeleteAsync(image.Url); } catch { }
            }

            foreach (var media in product.StoryMedia ?? new List<ProductStoryMedia>())
            {
                try { await _storage.DeleteAsync(media.Url); } catch { }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }


        private async Task ApplyActivePromotionsAsync(List<ProductResponseDto> dtos)
        {
            if (dtos.Count == 0) return;

            var productIds = dtos.Select(d => d.Id).ToList();
            var discounts = await GetActivePromotionDiscountsAsync(productIds);

            foreach (var dto in dtos)
            {
                if (!discounts.TryGetValue(dto.Id, out var discount))
                    continue;

                var promoPrice = Math.Round(dto.Price * (1 - discount / 100m), 2, MidpointRounding.AwayFromZero);
                if (promoPrice <= 0 || promoPrice >= dto.Price) continue;

                if (!dto.SalePrice.HasValue || promoPrice < dto.SalePrice.Value)
                {
                    dto.SalePrice = promoPrice;
                }
            }
        }

        private async Task<Dictionary<Guid, decimal>> GetActivePromotionDiscountsAsync(List<Guid> productIds)
        {
            var idSet = productIds.ToHashSet();
            if (idSet.Count == 0) return new Dictionary<Guid, decimal>();

            var now = DateTime.UtcNow;
            var promotions = await _context.Promotions
                .AsNoTracking()
                .Where(p => p.IsActive &&
                            p.StartDate <= now &&
                            p.EndDate >= now)
                .ToListAsync();

            var discounts = new Dictionary<Guid, decimal>();
            foreach (var promo in promotions)
            {
                foreach (var productId in promo.ProductIds)
                {
                    if (!idSet.Contains(productId)) continue;

                    if (!discounts.TryGetValue(productId, out var existing) || promo.DiscountPercentage > existing)
                    {
                        discounts[productId] = promo.DiscountPercentage;
                    }
                }
            }

            return discounts;
        }

        private bool IsImage(IFormFile file)
        {
            try
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                return new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" }.Contains(ext);
            }
            catch { return false; }
        }
    }
}
