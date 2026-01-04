using AutoMapper;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Responses; 
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceArtesanato.Services.Services
{
    public class OrderService : IOrderService
    {
        private readonly ArtesianDbContext _context;
        private readonly IMapper _mapper;
        private readonly IStripePaymentService _stripeService;

        public OrderService(
            ArtesianDbContext context,
            IMapper mapper,
            IStripePaymentService stripeService)
        {
            _context = context;
            _mapper = mapper;
            _stripeService = stripeService;
        }

        public async Task<List<OrderResponseDto>> GetByUserAsync(Guid userId, string role)
        {
            var query = _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .AsNoTracking();

            if (role == "Seller")
            {
                // Vendedor vê pedidos que contenham seus produtos
                query = query.Where(o => o.Items.Any(i => i.Product.SellerId == userId));
            }
            else if (role == "Admin")
            {
                // Admin vê tudo
            }
            else // Customer
            {
                query = query.Where(o => o.BuyerId == userId);
            }

            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return _mapper.Map<List<OrderResponseDto>>(orders);
        }

        public async Task<OrderResponseDto> GetByIdAsync(Guid orderId, Guid userId, string role)
        {
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Seller)
                .Include(o => o.Buyer) // Importante incluir o comprador
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) throw new KeyNotFoundException("Pedido não encontrado.");

            bool isBuyer = order.BuyerId == userId;
            // Verifica se o usuário é vendedor E se tem algum produto dele no pedido
            bool isSeller = role == "Seller" && order.Items.Any(i => i.Product.SellerId == userId);
            bool isAdmin = role == "Admin";

            if (!isBuyer && !isSeller && !isAdmin)
                throw new UnauthorizedAccessException("Sem permissão para visualizar este pedido.");

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
                // Verifica se o vendedor é dono de algum item no pedido
                bool isMyOrder = order.Items.Any(i => i.Product.SellerId == userId);
                if (!isMyOrder) throw new UnauthorizedAccessException("Este pedido não contém seus produtos.");

                order.TrackingCodes[userId] = trackingCode;
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
            if (order.Status == OrderStatus.Paid)
            {
                order.Status = OrderStatus.Sent;
            }

            // Truque para o EF Core detectar mudança no JSON/Dictionary
            order.TrackingCodes = new Dictionary<Guid, string>(order.TrackingCodes);

            await _context.SaveChangesAsync();
        }
    }
}