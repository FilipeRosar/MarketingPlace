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

        public async Task<CartDto> GetCartAsync(Guid customerId)
        {
            var key = $"{CartKeyPrefix}{customerId}";

            var redisValue = await _redis.StringGetAsync(key);
            if (redisValue.HasValue)
            {
                var cachedCart = JsonSerializer.Deserialize<CartDto>(redisValue!);
                if (cachedCart != null)
                {
                    return await EnrichWithProductData(cachedCart);
                }
            }

            var dbCart = await GetCartFromDbAsync(customerId);

            await SyncRedisAsync(customerId, dbCart);

            return dbCart;
        }

        public async Task AddItemAsync(Guid customerId, Guid productId, int quantity = 1)
        {
            if (quantity <= 0) throw new ArgumentException("Quantidade deve ser > 0");

            var product = await _context.Products.FindAsync(productId)
                ?? throw new KeyNotFoundException("Produto não encontrado");

            if (product.StockQuantity < quantity)
                throw new InvalidOperationException($"Estoque insuficiente: {product.StockQuantity} disponível(s)");

            var cart = await GetCartAsync(customerId);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (item == null)
            {
                cart.Items.Add(new CartItemDto
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    ProductImage = product.Images.FirstOrDefault(),
                    Price = product.Price,
                    Quantity = quantity,
                });
            }
            else
            {
                item.Quantity += quantity;
                if (item.Quantity > product.StockQuantity)
                    throw new InvalidOperationException("Quantidade total excede estoque disponível");
            }

            await SaveCartAsync(customerId, cart);
        }

        public async Task UpdateItemQuantityAsync(Guid customerId, Guid productId, int quantity)
        {
            if (quantity <= 0) throw new ArgumentException("Quantidade deve ser > 0");

            var cart = await GetCartAsync(customerId);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == productId)
                ?? throw new KeyNotFoundException("Item não está no carrinho");

            var product = await _context.Products.FindAsync(productId);
            if (product != null && product.StockQuantity < quantity)
                throw new InvalidOperationException("Estoque insuficiente");

            item.Quantity = quantity;
            await SaveCartAsync(customerId, cart);
        }

        public async Task RemoveItemAsync(Guid customerId, Guid productId)
        {
            var cart = await GetCartAsync(customerId);
            var removed = cart.Items.RemoveAll(i => i.ProductId == productId) > 0;

            if (removed)
            {
                await SaveCartAsync(customerId, cart);
            }
        }

        public async Task ClearCartAsync(Guid customerId)
        {
            var key = $"{CartKeyPrefix}{customerId}";
            await _redis.KeyDeleteAsync(key);

            var dbCart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (dbCart != null)
            {
                _context.CartItems.RemoveRange(dbCart.Items);
                _context.Carts.Remove(dbCart);
                await _context.SaveChangesAsync();
            }
        }


        private async Task<CartDto> GetCartFromDbAsync(Guid customerId)
        {
            var dbCart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (dbCart == null)
            {
                return new CartDto { CustomerId = customerId, Items = new() };
            }

            var dto = new CartDto
            {
                Id = dbCart.Id,
                CustomerId = dbCart.CustomerId,
                Items = dbCart.Items.Select(i => new CartItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ProductImage = i.Product.Images.FirstOrDefault(),
                    Price = i.Product.Price,
                    Quantity = i.Quantity
                }).ToList()
            };

            return dto;
        }

        private async Task SaveCartAsync(Guid customerId, CartDto cart)
        {
            await SyncRedisAsync(customerId, cart);

            var dbCart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (dbCart == null)
            {
                dbCart = new Cart { CustomerId = customerId };
                _context.Carts.Add(dbCart);
            }
            else
            {
                // Limpa itens antigos para substituir (simplificação)
                // Em produção, faríamos um merge inteligente
                _context.CartItems.RemoveRange(dbCart.Items);
            }

            // Recria itens
            dbCart.Items = cart.Items.Select(i => new CartItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList();

            await _context.SaveChangesAsync();
        }

        private async Task SyncRedisAsync(Guid customerId, CartDto cart)
        {
            var key = $"{CartKeyPrefix}{customerId}";
            var json = JsonSerializer.Serialize(cart);
            await _redis.StringSetAsync(key, json, TimeSpan.FromHours(24)); // Expira em 24h
        }

        private async Task<CartDto> EnrichWithProductData(CartDto cart)
        {
            // Garante que preço e nome estejam atualizados (se mudaram no banco desde o cache)
            foreach (var item in cart.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    item.ProductName = product.Name;
                    item.Price = product.Price;
                    item.ProductImage = product.Images.FirstOrDefault();
                }
            }
            return cart;
        }
    }
}