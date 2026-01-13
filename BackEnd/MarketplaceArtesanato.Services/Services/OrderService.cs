using AutoMapper;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
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
        private readonly IPlatformFeeService _platformFeeService;
        private readonly IPriceCalculationService _priceCalculationService;

        public OrderService(
            ArtesianDbContext context,
            IMapper mapper,
            IStripePaymentService stripeService,
            ILogger<OrderService> logger,
            IPlatformFeeService platformFeeService,
            IPriceCalculationService priceCalculationService)
        {
            _context = context;
            _mapper = mapper;
            _stripeService = stripeService;
            _logger = logger;
            _platformFeeService = platformFeeService;
            _priceCalculationService = priceCalculationService;
        }

        public async Task<List<OrderResponseDto>> GetByUserAsync(Guid userId, string role)
        {
            IQueryable<Order> query = _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Seller)
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
            var dtos = new List<OrderResponseDto>();

            foreach (var order in orders)
            {
                var dto = _mapper.Map<OrderResponseDto>(order);

                EnrichOrderDto(dto, order, sellerId, role);

                dtos.Add(dto);
            }

            return dtos;
        }

        public async Task<OrderResponseDto> GetByIdAsync(Guid orderId, Guid userId, string role)
        {
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Seller)
                .Include(o => o.Buyer)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) throw new KeyNotFoundException("Pedido não encontrado.");

            var sellerId = role == "Seller"
                ? await _context.Sellers.Where(s => s.UserId == userId).Select(s => s.Id).FirstOrDefaultAsync()
                : Guid.Empty;

            bool isBuyer = order.BuyerId == userId;
            bool isSeller = role == "Seller" && sellerId != Guid.Empty && order.Items.Any(i => i.Product.SellerId == sellerId);
            bool isAdmin = role == "Admin";

            if (!isBuyer && !isSeller && !isAdmin)
                throw new UnauthorizedAccessException("Sem permissão para visualizar este pedido.");

            var dto = _mapper.Map<OrderResponseDto>(order);

            EnrichOrderDto(dto, order, sellerId, role);

            return dto;
        }

        private void EnrichOrderDto(OrderResponseDto dto, Order order, Guid sellerId, string role)
        {
            if (role == "Seller" && sellerId != Guid.Empty)
            {
                if (order.TrackingCodes != null && order.TrackingCodes.TryGetValue(sellerId, out var code))
                {
                    dto.TrackingCode = code;
                    dto.TrackingCodes = new List<string> { code };
                }
            }
            else if (order.TrackingCodes != null && order.TrackingCodes.Count > 0)
            {
                dto.TrackingCode = order.TrackingCodes.First().Value;
                dto.TrackingCodes = order.TrackingCodes.Values.ToList();
            }

            if (!string.IsNullOrWhiteSpace(order.Carrier))
            {
                dto.Carrier = order.Carrier;
            }

            if (dto.TrackingCodes == null)
            {
                dto.TrackingCodes = new List<string>();
            }

            dto.ShippingCost = order.ShippingCost;
            dto.Subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            var sellerNames = order.Items
                .Where(i => i.Product?.Seller != null)
                .Select(i => i.Product.Seller.StoreName)
                .Distinct()
                .ToList();

            if (sellerNames.Count == 1)
            {
                dto.SellerName = sellerNames.First();
            }

            if (order.ShippingAddress != null)
            {
                dto.ShippingAddress = new ShippingAddressDTO
                {
                    Street = order.ShippingAddress.Street,
                    Number = order.ShippingAddress.Number,
                    Complement = order.ShippingAddress.Complement,
                    Neighborhood = order.ShippingAddress.Neighborhood,
                    City = order.ShippingAddress.City,
                    State = order.ShippingAddress.State,
                    ZipCode = order.ShippingAddress.ZipCode
                };
            }

            foreach (var itemDto in dto.Items)
            {
                var item = order.Items.FirstOrDefault(i => i.Id == itemDto.Id);
                if (item?.Product?.Seller != null)
                {
                    itemDto.SellerName = item.Product.Seller.StoreName;
                }
            }
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
                TrackingCodes = new Dictionary<Guid, string>(),

                // ✅ NOVO: Adicionar ShippingCost e Carrier do DTO
                ShippingCost = dto.ShippingCost,
                Carrier = dto.Carrier ?? "Correios",

                // ✅ NOVO: Adicionar ShippingAddress do DTO
                ShippingAddress = dto.ShippingAddress != null ? new ShippingAddress
                {
                    Street = dto.ShippingAddress.Street,
                    Number = dto.ShippingAddress.Number,
                    Complement = dto.ShippingAddress.Complement,
                    Neighborhood = dto.ShippingAddress.Neighborhood,
                    City = dto.ShippingAddress.City,
                    State = dto.ShippingAddress.State,
                    ZipCode = dto.ShippingAddress.ZipCode
                } : null
            };

            decimal orderTotal = 0;
            var sellerTotals = new Dictionary<Guid, decimal>();

            foreach (var itemDto in dto.Items)
            {
                var product = products.First(p => p.Id == itemDto.ProductId);

                // Validação de Estoque
                if (product.StockQuantity < itemDto.Quantity)
                    throw new InvalidOperationException($"Estoque insuficiente para o produto: {product.Name}");

                var priceResult = await _priceCalculationService.CalculateProductPriceAsync(product, buyerId);
                var unitPrice = priceResult.FinalPrice;

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Product = product,
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice,
                    ProductName = product.Name,
                    ProductImage = product.Images.FirstOrDefault()?.Url
                };

                order.Items.Add(orderItem);
                orderTotal += orderItem.UnitPrice * orderItem.Quantity;
                var sellerId = product.SellerId;
                var itemTotal = orderItem.UnitPrice * orderItem.Quantity;

                if (sellerTotals.ContainsKey(sellerId))
                    sellerTotals[sellerId] += itemTotal;
                else
                    sellerTotals[sellerId] = itemTotal;
            }

            var sellerRates = new Dictionary<Guid, decimal>();
            foreach (var (sellerId, sellerTotal) in sellerTotals)
            {
                var rate = await _platformFeeService.GetCommissionRateAsync(sellerId, sellerTotal);
                sellerRates[sellerId] = rate;
            }

            foreach (var item in order.Items)
            {
                var sellerId = item.Product.SellerId;
                var itemTotal = item.UnitPrice * item.Quantity;
                var commission = itemTotal * (sellerRates[sellerId] / 100m);

                if (order.SellerCommissions.ContainsKey(sellerId))
                    order.SellerCommissions[sellerId] += commission;
                else
                    order.SellerCommissions[sellerId] = commission;
            }

            order.TotalAmount = orderTotal + order.ShippingCost;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var paymentUrl = await _stripeService.CreateCheckoutSessionAsync(order, buyerId);

            return new CheckoutResponseResult
            {
                OrderId = order.Id,
                PaymentUrl = paymentUrl,
                Message = "Pedido criado. Redirecionando para pagamento..."
            };
        }

        public async Task UpdateTrackingAsync(Guid orderId, Guid userId, string role, string trackingCode, string carrier)
        {
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) throw new KeyNotFoundException("Pedido não encontrado.");

            if (order.TrackingCodes == null) order.TrackingCodes = new Dictionary<Guid, string>();

            if (role == "Seller")
            {
                var sellerId = await _context.Sellers
                    .Where(s => s.UserId == userId)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync();

                bool isMyOrder = sellerId != Guid.Empty && order.Items.Any(i => i.Product.SellerId == sellerId);
                if (!isMyOrder) throw new UnauthorizedAccessException("Este pedido não contém seus produtos.");

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
            order.Carrier = carrier?.Trim() ?? string.Empty;

            if (order.Status == OrderStatus.Confirmed)
            {
                order.Status = OrderStatus.Sent;
                order.ShippedAt = DateTime.UtcNow;
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

            if (order == null) throw new KeyNotFoundException("Pedido não encontrado.");

            var isBuyer = order.BuyerId == userId;
            var isAdmin = role == "Admin";

            if (!isBuyer && !isAdmin)
                throw new UnauthorizedAccessException("Sem permissão para cancelar este pedido.");

            if (order.Status == OrderStatus.Sent || order.Status == OrderStatus.Delivered)
                throw new InvalidOperationException("Pedido já enviado/entregue não pode ser cancelado.");

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
        public async Task<List<OrderDto>> GetMyOrdersAsync(Guid userId)
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Seller)
                .Where(o => o.BuyerId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(o => MapToDTO(o)).ToList();
        }

        private OrderDto MapToDTO(Order order)
        {
            var dto = new OrderDto
            {
                Id = order.Id,
                BuyerId = order.BuyerId,
                CreatedAt = order.CreatedAt,
                ShippedAt = order.ShippedAt,
                TotalAmount = order.TotalAmount,

                // ✅ Frete e subtotal
                ShippingCost = order.ShippingCost,
                SubTotal = order.Subtotal,

                StripeSessionId = order.StripeSessionId,
                StripePaymentIntentId = order.StripePaymentIntentId,
                Status = order.Status,
                StatusText = order.Status.ToString(),
                Carrier = order.Carrier,

                TrackingCode = order.TrackingCodes?.Values.FirstOrDefault(),
                TrackingCodes = order.TrackingCodes?.Values.ToList(),

                // ✅ Endereço
                ShippingAddress = order.ShippingAddress == null ? null : new ShippingAddressDTO
                {
                    Street = order.ShippingAddress.Street,
                    Number = order.ShippingAddress.Number,
                    Complement = order.ShippingAddress.Complement,
                    Neighborhood = order.ShippingAddress.Neighborhood,
                    City = order.ShippingAddress.City,
                    State = order.ShippingAddress.State,
                    ZipCode = order.ShippingAddress.ZipCode
                },

                SellerName = GetSellerName(order),

                Items = order.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    ProductImage = i.ProductImage,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,

                    SellerName = i.Product?.Seller?.StoreName ?? "Vendedor não disponível"
                }).ToList()
            };

            return dto;
        }

        private string? GetSellerName(Order order)
        {
            var sellerNames = order.Items
                .Where(i => i.Product?.Seller != null)
                .Select(i => i.Product.Seller.StoreName)
                .Distinct()
                .ToList();

            return sellerNames.Count == 1 ? sellerNames.First() : null;
        }
    }
}