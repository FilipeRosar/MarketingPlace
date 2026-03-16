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

        /* =========================================================
           CONSULTAS DE PEDIDOS
        ========================================================= */

        public async Task<List<OrderResponseDto>> GetByUserAsync(Guid userId, string role)
        {
            try
            {
                Guid sellerId = Guid.Empty;

                if (role == "Seller")
                {
                    sellerId = await _context.Sellers
                        .Where(s => s.UserId == userId)
                        .Select(s => s.Id)
                        .FirstOrDefaultAsync();

                    if (sellerId == Guid.Empty)
                        return new List<OrderResponseDto>();
                }

                // Build the base query with all includes first
                IQueryable<Order> query = _context.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                            .ThenInclude(p => p.Seller)
                    .AsNoTracking();

                // Apply role-based filtering
                if (role == "Seller")
                {
                    query = query.Where(o =>
                        o.Items.Any(i => i.SellerId == sellerId));
                }
                else if (role != "Admin")
                {
                    query = query.Where(o => o.BuyerId == userId);
                }

                // Filter out invalid orders
                query = query.Where(o => o.BuyerId != null && o.BuyerId != Guid.Empty);

                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync();

                var result = new List<OrderResponseDto>();

                foreach (var order in orders)
                {
                    try
                    {
                        var dto = _mapper.Map<OrderResponseDto>(order);
                        EnrichOrderDto(dto, order, sellerId, role);
                        result.Add(dto);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Erro ao mapear pedido {OrderId}", order?.Id);
                        // Skip invalid orders
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar pedidos do usuário {UserId}", userId);
                throw;
            }
        }

        public async Task<OrderResponseDto> GetByIdAsync(Guid orderId, Guid userId, string role)
        {
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
                .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Seller)
                .Include(o => o.Buyer)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new KeyNotFoundException("Pedido não encontrado.");

            var sellerId = role == "Seller"
                ? await _context.Sellers
                    .Where(s => s.UserId == userId)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync()
                : Guid.Empty;

            bool autorizado =
                role == "Admin" ||
                order.BuyerId == userId ||
                (role == "Seller" &&
                 sellerId != Guid.Empty &&
                 order.Items.Any(i => i.Product.SellerId == sellerId));

            if (!autorizado)
                throw new UnauthorizedAccessException("Sem permissão para visualizar este pedido.");

            var dto = _mapper.Map<OrderResponseDto>(order);
            EnrichOrderDto(dto, order, sellerId, role);

            return dto;
        }

        /* =========================================================
           CRIAÇÃO DE PEDIDO (CHECKOUT)
        ========================================================= */

        public async Task<CheckoutResponseResult> CreateOrderAsync(Guid buyerId, CheckoutRequestDto dto)
        {
            var buyer = await _context.Users.FindAsync(buyerId)
                ?? throw new KeyNotFoundException("Comprador não encontrado.");

            var productIds = dto.Items.Select(i => i.ProductId).ToList();

            var products = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Seller)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            if (products.Count != productIds.Count)
                throw new InvalidOperationException("Produto inválido ou indisponível.");

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
                ShippingCost = dto.ShippingCost,
                Carrier = dto.Carrier ?? "Correios",
                ShippingAddress = dto.ShippingAddress == null ? null : new ShippingAddress
                {
                    Street = dto.ShippingAddress.Street,
                    Number = dto.ShippingAddress.Number,
                    Complement = dto.ShippingAddress.Complement,
                    Neighborhood = dto.ShippingAddress.Neighborhood,
                    City = dto.ShippingAddress.City,
                    State = dto.ShippingAddress.State,
                    ZipCode = dto.ShippingAddress.ZipCode
                }
            };

            decimal orderTotal = 0;
            var sellerTotals = new Dictionary<Guid, decimal>();

            foreach (var item in dto.Items)
            {
                var product = products.First(p => p.Id == item.ProductId);

                if (product.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Estoque insuficiente: {product.Name}");

                var price = await _priceCalculationService
                    .CalculateProductPriceAsync(product, buyerId);

                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Product = product,
                    Quantity = item.Quantity,
                    UnitPrice = price.FinalPrice,
                    ProductName = product.Name,
                    ProductImage = product.Images.FirstOrDefault()?.Url,
                    SellerName = product.Seller?.StoreName ?? string.Empty
                };

                order.Items.Add(orderItem);

                var totalItem = orderItem.UnitPrice * orderItem.Quantity;
                orderTotal += totalItem;

                if (!sellerTotals.ContainsKey(product.SellerId))
                    sellerTotals[product.SellerId] = 0;

                sellerTotals[product.SellerId] += totalItem;
            }

            foreach (var (sellerId, total) in sellerTotals)
            {
                var rate = await _platformFeeService
                    .GetCommissionRateAsync(sellerId, total);

                order.SellerCommissions[sellerId] = total * (rate / 100m);
            }

            order.TotalAmount = orderTotal + order.ShippingCost;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var paymentUrl = await _stripeService
                .CreateCheckoutSessionAsync(order, buyerId);

            return new CheckoutResponseResult
            {
                OrderId = order.Id,
                PaymentUrl = paymentUrl,
                Message = "Pedido criado com sucesso."
            };
        }

        /* =========================================================
           ATUALIZAÇÃO DE RASTREAMENTO
        ========================================================= */

        public async Task UpdateTrackingAsync(
            Guid orderId,
            Guid userId,
            string role,
            string trackingCode,
            string carrier)
        {
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new KeyNotFoundException("Pedido não encontrado.");

            if (order.TrackingCodes == null)
                order.TrackingCodes = new Dictionary<Guid, string>();

            if (role == "Seller")
            {
                var sellerId = await _context.Sellers
                    .Where(s => s.UserId == userId)
                    .Select(s => s.Id)
                    .FirstOrDefaultAsync();

                if (!order.Items.Any(i => i.Product.SellerId == sellerId))
                    throw new UnauthorizedAccessException();

                order.TrackingCodes[sellerId] = trackingCode;
            }
            else if (role == "Admin")
            {
                var sellerId = order.Items.First().Product.SellerId;
                order.TrackingCodes[sellerId] = trackingCode;
            }
            else
            {
                throw new UnauthorizedAccessException();
            }

            order.Carrier = carrier;

            if (order.Status == OrderStatus.Confirmed)
            {
                order.Status = OrderStatus.Sent;
                order.ShippedAt = DateTime.UtcNow;
            }

            order.TrackingCodes = new Dictionary<Guid, string>(order.TrackingCodes);

            await _context.SaveChangesAsync();
        }

        /* =========================================================
           CANCELAMENTO / REEMBOLSO
        ========================================================= */

        public async Task CancelOrderAsync(Guid orderId, Guid userId, string role)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new KeyNotFoundException();

            if (order.BuyerId != userId && role != "Admin")
                throw new UnauthorizedAccessException();

            if (order.Status is OrderStatus.Sent or OrderStatus.Delivered)
                throw new InvalidOperationException();

            if (order.Status == OrderStatus.Confirmed &&
                !string.IsNullOrEmpty(order.StripePaymentIntentId))
            {
                var refundService = new RefundService();
                await refundService.CreateAsync(new RefundCreateOptions
                {
                    PaymentIntent = order.StripePaymentIntentId
                });

                order.Status = OrderStatus.Refunded;
            }
            else
            {
                order.Status = OrderStatus.Canceled;
            }

            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private void EnrichOrderDto(
            OrderResponseDto dto,
            Order order,
            Guid sellerId,
            string role)
        {
            dto.ShippingCost = order.ShippingCost;
            dto.Subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
            dto.Carrier = order.Carrier;

            if (order.TrackingCodes != null)
            {
                if (role == "Seller" && sellerId != Guid.Empty &&
                    order.TrackingCodes.TryGetValue(sellerId, out var code))
                {
                    dto.TrackingCode = code;
                    dto.TrackingCodes = new List<string> { code };
                }
                else
                {
                    dto.TrackingCodes = order.TrackingCodes.Values.ToList();
                    dto.TrackingCode = dto.TrackingCodes.FirstOrDefault();
                }
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

            foreach (var item in dto.Items)
            {
                var entity = order.Items.First(i => i.Id == item.Id);
                item.SellerName = entity.SellerName ?? entity.Product?.Seller?.StoreName;
            }
        }
    }
}