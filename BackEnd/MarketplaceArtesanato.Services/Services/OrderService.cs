using AutoMapper;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Responses; 
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;

namespace MarketplaceArtesanato.Services.Services
{
    public class OrderService : IOrderService
    {
        private readonly ArtesianDbContext _context;
        private readonly IMapper _mapper;
        private readonly IStripePaymentService _stripeService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            ArtesianDbContext context,
            IMapper mapper,
            IStripePaymentService stripeService,
            ILogger<OrderService> logger)
        {
            _context = context;
            _mapper = mapper;
            _stripeService = stripeService;
            _logger = logger;
        }

        public async Task<List<OrderResponseDto>> GetByUserAsync(Guid userId, string role)
{
    var query = _context.Orders
        .Include(o => o.Items).ThenInclude(i => i.Product)
        .AsNoTracking();

    Guid sellerId = Guid.Empty;

    if (role == "Seller")
    {
        sellerId = await _context.Sellers
            .Where(s => s.UserId == userId)
            .Select(s => s.Id)
            .FirstOrDefaultAsync();

        if (sellerId == Guid.Empty)
        {
            return new List<OrderResponseDto>();
        }

        query = query.Where(o => o.Items.Any(i => i.Product.SellerId == sellerId));
    }
    else if (role == "Admin")
    {
        // Admin ve tudo
    }
    else // Customer
    {
        query = query.Where(o => o.BuyerId == userId);
    }

    var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
    var dtos = _mapper.Map<List<OrderResponseDto>>(orders);

    if (role == "Seller" && sellerId != Guid.Empty)
    {
        for (int i = 0; i < orders.Count && i < dtos.Count; i++)
        {
            var order = orders[i];
            if (order.TrackingCodes != null && order.TrackingCodes.TryGetValue(sellerId, out var code))
            {
                dtos[i].TrackingCode = code;
            }
        }
    }

    return dtos;
}

        public async Task<OrderResponseDto> GetByIdAsync(Guid orderId, Guid userId, string role)
{
    var order = await _context.Orders
        .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
        .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Seller)
        .Include(o => o.Buyer) // Importante incluir o comprador
        .FirstOrDefaultAsync(o => o.Id == orderId);

    if (order == null) throw new KeyNotFoundException("Pedido nao encontrado.");

    var sellerId = role == "Seller"
        ? await _context.Sellers.Where(s => s.UserId == userId).Select(s => s.Id).FirstOrDefaultAsync()
        : Guid.Empty;

    bool isBuyer = order.BuyerId == userId;
    // Verifica se o usuario e vendedor e se tem algum produto dele no pedido
    bool isSeller = role == "Seller" && sellerId != Guid.Empty && order.Items.Any(i => i.Product.SellerId == sellerId);
    bool isAdmin = role == "Admin";

    if (!isBuyer && !isSeller && !isAdmin)
        throw new UnauthorizedAccessException("Sem permissao para visualizar este pedido.");

    return _mapper.Map<OrderResponseDto>(order);
}

        public async Task<CheckoutResponseResult> CreateOrderAsync(Guid buyerId, CheckoutRequestDto dto)
        {
            var buyer = await _context.Users.FindAsync(buyerId);
            if (buyer == null) throw new KeyNotFoundException("Comprador não encontrado.");

            // 1. Validar e Buscar Produtos
            var productIds = dto.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Include(p => p.Seller)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            if (products.Count != productIds.Count)
                throw new InvalidOperationException("Algum produto do carrinho não foi encontrado ou está indisponível.");

            // 2. Construir o Pedido
            var order = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                Buyer = buyer,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>(),
                SellerCommissions = new Dictionary<Guid, decimal>(),
                TrackingCodes = new Dictionary<Guid, string>()
            };

            decimal orderTotal = 0;

            foreach (var itemDto in dto.Items)
            {
                var product = products.First(p => p.Id == itemDto.ProductId);

                // Validação de Estoque
                if (product.StockQuantity < itemDto.Quantity)
                    throw new InvalidOperationException($"Estoque insuficiente para o produto: {product.Name}");

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Product = product,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,
                    ProductName = product.Name,
                    ProductImage = product.Images.FirstOrDefault()?.Url
                };

                order.Items.Add(orderItem);
                orderTotal += orderItem.UnitPrice * orderItem.Quantity;

                // Cálculo de Comissão (Split)
                var sellerId = product.SellerId;
                var itemTotal = orderItem.UnitPrice * orderItem.Quantity;
                var commission = itemTotal * (product.Seller.CommissionRate / 100m);

                if (order.SellerCommissions.ContainsKey(sellerId))
                    order.SellerCommissions[sellerId] += commission;
                else
                    order.SellerCommissions[sellerId] = commission;
            }

            order.TotalAmount = orderTotal;

            // 3. Persistir Pedido (Status Pendente)
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // 4. Gerar Sessão de Pagamento no Stripe
            var paymentUrl = await _stripeService.CreateCheckoutSessionAsync(order, buyerId);

            return new CheckoutResponseResult
            {
                OrderId = order.Id,
                PaymentUrl = paymentUrl,
                Message = "Pedido criado. Redirecionando para pagamento..."
            };
        }

        public async Task UpdateTrackingAsync(Guid orderId, Guid userId, string role, string trackingCode)
        {
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) throw new KeyNotFoundException("Pedido não encontrado.");

            // Garante inicialização do dicionário se for nulo
            if (order.TrackingCodes == null) order.TrackingCodes = new Dictionary<Guid, string>();

            if (role == "Seller")
{
    var sellerId = await _context.Sellers
        .Where(s => s.UserId == userId)
        .Select(s => s.Id)
        .FirstOrDefaultAsync();

    bool isMyOrder = sellerId != Guid.Empty && order.Items.Any(i => i.Product.SellerId == sellerId);
    if (!isMyOrder) throw new UnauthorizedAccessException("Este pedido nao contem seus produtos.");

    order.TrackingCodes[sellerId] = trackingCode;
}
else if (role == "Admin")
            {
                // Admin: Se quiser ser específico, o DTO deveria ter o SellerId alvo.
                // Fallback: pega o primeiro vendedor encontrado
                var targetSellerId = order.Items.First().Product.SellerId;
                order.TrackingCodes[targetSellerId] = trackingCode;
            }
            else
            {
                throw new UnauthorizedAccessException("Apenas Vendedores ou Admins podem atualizar rastreio.");
            }

            // Regra de Negócio Simplificada: Se já foi pago e adicionou rastreio, muda para Enviado
            if (order.Status == OrderStatus.Confirmed)
            {
                order.Status = OrderStatus.Sent;
            }

            // Truque para o EF Core detectar mudança no JSON/Dictionary
            order.TrackingCodes = new Dictionary<Guid, string>(order.TrackingCodes);

            await _context.SaveChangesAsync();
        }

        public async Task CancelOrderAsync(Guid orderId, Guid userId, string role)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) throw new KeyNotFoundException("Pedido nÆo encontrado.");

            var isBuyer = order.BuyerId == userId;
            var isAdmin = role == "Admin";

            if (!isBuyer && !isAdmin)
                throw new UnauthorizedAccessException("Sem permissÆo para cancelar este pedido.");

            if (order.Status == OrderStatus.Sent || order.Status == OrderStatus.Delivered)
                throw new InvalidOperationException("Pedido j  enviado/entregue nÆo pode ser cancelado.");

            if (order.Status == OrderStatus.Canceled || order.Status == OrderStatus.Refunded)
                return;

            if (order.Status == OrderStatus.Confirmed && !string.IsNullOrWhiteSpace(order.StripePaymentIntentId))
            {
                var refundService = new RefundService();
                try
                {
                    await refundService.CreateAsync(new RefundCreateOptions
                    {
                        PaymentIntent = order.StripePaymentIntentId
                    });

                    order.Status = OrderStatus.Refunded;
                }
                catch (StripeException ex)
                {
                    _logger.LogError(ex, "Falha ao reembolsar pedido {OrderId} (PaymentIntent {PaymentIntentId})", order.Id, order.StripePaymentIntentId);
                    throw;
                }
            }
            else
            {
                order.Status = OrderStatus.Canceled;
            }

            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}

