using AutoMapper;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Text.Json;

namespace MarketplaceArtesanato.Services.Services
{
    public class CartService : ICartService
    {
        private readonly ArtesianDbContext _context;
        private readonly IDatabase _redis;
        private readonly IMapper _mapper;
        private readonly IPriceCalculationService _priceCalculationService;
        private const string CartKeyPrefix = "cart:";

        public CartService(
            ArtesianDbContext context,
            IConnectionMultiplexer redis,
            IMapper mapper,
            IPriceCalculationService priceCalculationService)
        {
            _context = context;
            _redis = redis.GetDatabase();
            _mapper = mapper;
            _priceCalculationService = priceCalculationService;
        }

        public async Task<CartDto> GetCartAsync(Guid userId, string? couponCode = null)
        {
            var key = $"{CartKeyPrefix}{userId}";

            try
            {
                var redisValue = await _redis.StringGetAsync(key);
                if (redisValue.HasValue)
                {
                    var cachedCart = JsonSerializer.Deserialize<CartDto>(redisValue!);
                    if (cachedCart != null)
                    {
                        return await EnrichWithPriceData(cachedCart, userId, couponCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REDIS ERRO - GET] Ignorando cache: {ex.Message}");
            }

            var dbCart = await GetCartFromDbAsync(userId, couponCode);
            await SyncRedisAsync(userId, dbCart);

            return dbCart;
        }

        public async Task AddItemAsync(Guid userId, Guid productId, int quantity = 1)
        {
            if (quantity <= 0) throw new ArgumentException("Quantidade deve ser > 0");

            var saved = false;
            for (var attempt = 0; attempt < 2 && !saved; attempt++)
            {
                var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId)
             ?? throw new KeyNotFoundException("Produto não encontrado");

                if (product.StockQuantity < quantity)
                    throw new InvalidOperationException($"Estoque insuficiente.");

                var cart = await _context.Carts
                    .Include(c => c.Items)
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart != null && cart.IsDeleted)
                {
                    cart.IsDeleted = false;
                    cart.UpdatedAt = DateTime.UtcNow;
                    await _context.Carts
                        .IgnoreQueryFilters()
                        .Where(c => c.Id == cart.Id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(c => c.IsDeleted, false)
                            .SetProperty(c => c.UpdatedAt, DateTime.UtcNow));

                    _context.ChangeTracker.Clear();
                    cart = await _context.Carts
                        .Include(c => c.Items)
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(c => c.UserId == userId);
                }

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId,
                        Items = new List<CartItem>()
                    };
                    _context.Carts.Add(cart);
                }
                else
                {
                    if (cart.IsDeleted)
                    {
                        cart.IsDeleted = false;
                    }
                    cart.UpdatedAt = DateTime.UtcNow;
                }

                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

                if (item == null)
                {
                    cart.Items.Add(new CartItem
                    {
                        Id = Guid.NewGuid(),
                        CartId = cart.Id,
                        ProductId = productId,
                        Quantity = quantity
                    });
                }
                else
                {
                    item.Quantity += quantity;
                }

                try
                {
                    await _context.SaveChangesAsync();
                    saved = true;
                }
                catch (DbUpdateConcurrencyException ex) when (attempt == 0)
                {
                    Console.WriteLine($"[CONCURRENCY] Recarregando carrinho: {ex.Message}");
                    _context.ChangeTracker.Clear();
                }
                catch (DbUpdateConcurrencyException ex) when (attempt == 1)
                {
                    Console.WriteLine($"[CONCURRENCY] Recriando carrinho: {ex.Message}");
                    _context.ChangeTracker.Clear();

                    await _context.Carts
                        .IgnoreQueryFilters()
                        .Where(c => c.UserId == userId)
                        .ExecuteDeleteAsync();

                    var fallbackCart = new Cart
                    {
                        UserId = userId,
                        Items = new List<CartItem>()
                    };
                    fallbackCart.Items.Add(new CartItem
                    {
                        Id = Guid.NewGuid(),
                        CartId = fallbackCart.Id,
                        ProductId = productId,
                        Quantity = quantity
                    });

                    _context.Carts.Add(fallbackCart);
                    await _context.SaveChangesAsync();
                    saved = true;
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"[ERRO DB] Falha ao salvar carrinho: {ex.Message}");
                    if (ex.InnerException != null) Console.WriteLine($"[INNER] {ex.InnerException.Message}");
                    throw;
                }
            }

            if (saved)
            {
                var cartDto = await GetCartFromDbAsync(userId, null);
                await SyncRedisAsync(userId, cartDto);
            }
        }

        public async Task UpdateItemQuantityAsync(Guid userId, Guid productId, int quantity)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null) throw new KeyNotFoundException("Carrinho não encontrado");

            if (cart.IsDeleted) cart.IsDeleted = false;

            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) throw new KeyNotFoundException("Item não encontrado no carrinho");

            if (quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
            }

            await _context.SaveChangesAsync();

            var cartDto = await GetCartFromDbAsync(userId, null);
            await SyncRedisAsync(userId, cartDto);
        }

        public async Task RemoveItemAsync(Guid userId, Guid productId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    _context.CartItems.Remove(item);
                    await _context.SaveChangesAsync();
                }

                var cartDto = await GetCartFromDbAsync(userId, null);
                await SyncRedisAsync(userId, cartDto);
            }
        }

        public async Task ClearCartAsync(Guid userId)
        {
            try
            {
                var key = $"{CartKeyPrefix}{userId}";
                await _redis.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REDIS ERRO - DELETE] {ex.Message}");
            }

            var dbCart = await _context.Carts
                .Include(c => c.Items)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (dbCart != null)
            {
                _context.CartItems.RemoveRange(dbCart.Items);
                dbCart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        private async Task<CartDto> GetCartFromDbAsync(Guid userId, string? couponCode = null)
        {
            var dbCart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (dbCart == null || (dbCart.IsDeleted && !dbCart.Items.Any()))
            {
                return new CartDto { CustomerId = userId, Items = new() };
            }

            var products = dbCart.Items
                .Where(i => i.Product != null)
                .Select(i => i.Product!)
                .ToList();

            var priceResults = await _priceCalculationService.CalculateBulkPricesAsync(
                products,
                userId,
                couponCode);

            var dto = new CartDto
            {
                Id = dbCart.Id,
                CustomerId = dbCart.UserId,
                Items = dbCart.Items.Select(i =>
                {
                    var product = i.Product;
                    var priceResult = priceResults.GetValueOrDefault(i.ProductId);

                    return new CartItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = product?.Name ?? "Produto Indisponível",
                        ProductImage = product?.Images != null && product.Images.Any() ? product.Images[0].Url : null,

                        OriginalPrice = priceResult?.BasePrice ?? product?.Price ?? 0,
                        Price = priceResult?.FinalPrice ?? product?.Price ?? 0,
                        HasDiscount = priceResult?.HasAnyDiscount ?? false,
                        DiscountDetails = priceResult?.Adjustments.Select(a => new DiscountDetailDto
                        {
                            Type = a.Type.ToString(),
                            Description = a.Description,
                            Amount = a.Amount,
                            Percentage = a.Percentage
                        }).ToList() ?? new(),

                        Weight = product?.Weight ?? 0,
                        Width = product?.Width ?? 0,
                        Height = product?.Height ?? 0,
                        Length = product?.Length ?? 0,
                        Quantity = i.Quantity,
                        SellerId = product?.SellerId ?? Guid.Empty
                    };
                }).ToList()
            };

            return dto;
        }

        /// <summary>
        /// Enriquece dados do cache com preços atualizados
        /// </summary>
        private async Task<CartDto> EnrichWithPriceData(CartDto cart, Guid userId, string? couponCode)
        {
            var productIds = cart.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            var priceResults = await _priceCalculationService.CalculateBulkPricesAsync(
                products,
                userId,
                couponCode);

            foreach (var item in cart.Items)
            {
                if (priceResults.TryGetValue(item.ProductId, out var priceResult))
                {
                    item.OriginalPrice = priceResult.BasePrice;
                    item.Price = priceResult.FinalPrice;
                    item.HasDiscount = priceResult.HasAnyDiscount;
                    item.DiscountDetails = priceResult.Adjustments.Select(a => new DiscountDetailDto
                    {
                        Type = a.Type.ToString(),
                        Description = a.Description,
                        Amount = a.Amount,
                        Percentage = a.Percentage
                    }).ToList();
                }

                // Atualiza dados básicos do produto
                var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                if (product != null)
                {
                    item.ProductName = product.Name;
                    item.ProductImage = product.Images != null && product.Images.Any() ? product.Images[0].Url : null;
                    item.Weight = product.Weight;
                    item.Width = product.Width;
                    item.Height = product.Height;
                    item.Length = product.Length;
                }
            }

            return cart;
        }

        private async Task SyncRedisAsync(Guid userId, CartDto cart)
        {
            try
            {
                var key = $"{CartKeyPrefix}{userId}";
                var json = JsonSerializer.Serialize(cart);
                await _redis.StringSetAsync(key, json, TimeSpan.FromHours(24));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REDIS ERRO - SYNC] {ex.Message}");
            }
        }
    }
}