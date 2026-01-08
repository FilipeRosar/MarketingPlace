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
        private const string CartKeyPrefix = "cart:";

        public CartService(
            ArtesianDbContext context,
            IConnectionMultiplexer redis,
            IMapper mapper)
        {
            _context = context;
            _redis = redis.GetDatabase();
            _mapper = mapper;
        }

        public async Task<CartDto> GetCartAsync(Guid userId)
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
                        return await EnrichWithProductData(cachedCart);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REDIS ERRO - GET] Ignorando cache: {ex.Message}");
            }

            var dbCart = await GetCartFromDbAsync(userId);

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
                var cartDto = await GetCartFromDbAsync(userId);
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

            var cartDto = await GetCartFromDbAsync(userId);
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

                var cartDto = await GetCartFromDbAsync(userId);
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

        private async Task<CartDto> GetCartFromDbAsync(Guid userId)
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

            var dto = new CartDto
            {
                Id = dbCart.Id,
                CustomerId = dbCart.UserId, 
                Items = dbCart.Items.Select(i => new CartItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "Produto Indisponível",
                    ProductImage = i.Product?.Images != null && i.Product.Images.Any() ? i.Product.Images[0].Url : null,
                    Price = i.Product?.Price ?? 0,
                    Weight = i.Product?.Weight ?? 0,
                    Width = i.Product?.Width ?? 0,
                    Height = i.Product?.Height ?? 0,
                    Length = i.Product?.Length ?? 0,
                    Quantity = i.Quantity,
                    SellerId = i.Product?.SellerId ?? Guid.Empty
                }).ToList()
            };

            return dto;
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

        private async Task<CartDto> EnrichWithProductData(CartDto cart)
        {
            foreach (var item in cart.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    item.ProductName = product.Name;
                    item.Price = product.Price;
                    item.ProductImage = product.Images != null && product.Images.Any() ? product.Images[0].Url : null;
                    item.Weight = product.Weight;
                    item.Width = product.Width;
                    item.Height = product.Height;
                    item.Length = product.Length;
                }
            }
            return cart;
        }
    }
}