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

            // Tenta buscar do Redis (com proteção contra falhas)
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

            // Fallback para o Banco de Dados
            var dbCart = await GetCartFromDbAsync(customerId);

            // Tenta atualizar o cache (sem quebrar se falhar)
            await SyncRedisAsync(customerId, dbCart);

            return dbCart;
        }

        public async Task AddItemAsync(Guid customerId, Guid productId, int quantity = 1)
        {
            if (quantity <= 0) throw new ArgumentException("Quantidade deve ser > 0");

            var product = await _context.Products.FindAsync(productId)
                ?? throw new KeyNotFoundException("Produto não encontrado");

            if (product.StockQuantity < quantity)
                throw new InvalidOperationException($"Estoque insuficiente.");

            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                cart = new Cart 
                { 
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Items = new List<CartItem>()
                };
                _context.Carts.Add(cart);
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

            await _context.SaveChangesAsync();
            
            // Atualiza cache sem bloquear erro
            var cartDto = await GetCartFromDbAsync(customerId);
            await SyncRedisAsync(customerId, cartDto);
        }

        public async Task UpdateItemQuantityAsync(Guid customerId, Guid productId, int quantity)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null) throw new KeyNotFoundException("Carrinho não encontrado");

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
            
            var cartDto = await GetCartFromDbAsync(customerId);
            await SyncRedisAsync(customerId, cartDto);
        }

        public async Task RemoveItemAsync(Guid customerId, Guid productId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    _context.CartItems.Remove(item);
                    await _context.SaveChangesAsync();
                }
                
                var cartDto = await GetCartFromDbAsync(customerId);
                await SyncRedisAsync(customerId, cartDto);
            }
        }

        public async Task ClearCartAsync(Guid customerId)
        {
            // Limpa Redis (Safe)
            try
            {
                var key = $"{CartKeyPrefix}{customerId}";
                await _redis.KeyDeleteAsync(key);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"[REDIS ERRO - DELETE] {ex.Message}");
            }

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
                .ThenInclude(p => p.Images) 
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (dbCart == null) return new CartDto { CustomerId = customerId, Items = new() };

            var dto = new CartDto
            {
                Id = dbCart.Id,
                CustomerId = dbCart.CustomerId,
                Items = dbCart.Items.Select(i => new CartItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    ProductImage = i.Product.Images != null && i.Product.Images.Any() ? i.Product.Images[0] : null,
                    Price = i.Product.Price,
                    Quantity = i.Quantity,
                    SellerId = i.Product.SellerId
                }).ToList()
            };

            return dto;
        }
        
        private async Task SyncRedisAsync(Guid customerId, CartDto cart)
        {
            try
            {
                var key = $"{CartKeyPrefix}{customerId}";
                var json = JsonSerializer.Serialize(cart);
                await _redis.StringSetAsync(key, json, TimeSpan.FromHours(24));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[REDIS ERRO - SYNC] Não foi possível salvar no cache: {ex.Message}");
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
                    // Atualiza imagem caso tenha mudado
                    item.ProductImage = product.Images != null && product.Images.Any() ? product.Images[0] : null;
                }
            }
            return cart;
        }
    }
}