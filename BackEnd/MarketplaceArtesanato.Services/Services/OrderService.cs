using AutoMapper;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Data;
using MarketplaceArtesanato.Data.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceArtesanato.Application.Services;

public class OrderService : IOrderService
{
    private readonly ArtesianDbContext _context;
    private readonly ICartService _cartService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IMapper _mapper;
    private const decimal CommissionRate = 0.12m; 

    public OrderService(
        ArtesianDbContext context,
        ICartService cartService,
        IPublishEndpoint publishEndpoint,
        IMapper mapper)
    {
        _context = context;
        _cartService = cartService;
        _publishEndpoint = publishEndpoint;
        _mapper = mapper;
    }

    public async Task<OrderDto> CreateFromCartAsync(Guid customerId, CheckoutRequestDto dto)
    {
        var cart = await _cartService.GetCartAsync(customerId);
        if (!cart.Items.Any())
            throw new InvalidOperationException("Carrinho vazio.");

        foreach (var item in cart.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null || product.StockQuantity < item.Quantity)
                throw new InvalidOperationException($"Estoque insuficiente para {product?.Name}");
        }

        var order = new Order
        {
            BuyerId = customerId,
            Items = cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.Price
            }).ToList()
        };

        // Cálculo de comissão por Seller
        var commissions = new Dictionary<Guid, decimal>();
        foreach (var item in order.Items)
        {
            var product = await _context.Products
                .Include(p => p.Seller)
                .FirstAsync(p => p.Id == item.ProductId);

            var subtotal = item.UnitPrice * item.Quantity;
            var commission = subtotal * CommissionRate;

            if (commissions.ContainsKey(product.SellerId))
                commissions[product.SellerId] += commission;
            else
                commissions[product.SellerId] = commission;

            item.Product = product;
        }

        order.SellerCommissions = commissions;
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Limpar carrinho
        await _cartService.ClearCartAsync(customerId);

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<bool> ProcessPaymentAsync(PaymentProcessedEvent evt)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == evt.OrderId);

        if (order == null || order.Status != OrderStatus.Pending)
            return false;

        order.Status = OrderStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;
        order.StripeSessionId = evt.StripeSessionId;
        order.StripePaymentIntentId = evt.PaymentIntentId;

        foreach (var item in order.Items)
        {
            item.Product.StockQuantity -= item.Quantity;
        }

        await _context.SaveChangesAsync();

        await _publishEndpoint.Publish(new OrderPaidEvent
        {
            OrderId = order.Id,
            Total = order.TotalAmount,
            SellerCommissions = order.SellerCommissions
        });

        return true;
    }
    public async Task ProcessCommissionAsync(Order order)
    {
        var commissions = new Dictionary<Guid, decimal>();

        foreach (var item in order.Items)
        {
            var commission = item.Subtotal * 0.12m; // 12%
            if (commissions.ContainsKey(item.SellerId))
                commissions[item.SellerId] += commission;
            else
                commissions[item.SellerId] = commission;
        }

        order.SellerCommissions = commissions;
        await _context.SaveChangesAsync();
    }
    public async Task<OrderDto?> GetByIdAsync(Guid orderId, Guid userId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Seller)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return null;

        var isCustomer = order.BuyerId == userId;
        var isSeller = order.Items.Any(i => i.Product.SellerId == userId);

        if (!isCustomer && !isSeller)
            throw new UnauthorizedAccessException();

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<IEnumerable<OrderDto>> GetByCustomerAsync(Guid customerId)
    {
        var orders = await _context.Orders
            .Where(o => o.BuyerId == customerId)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .ToListAsync();

        return _mapper.Map<List<OrderDto>>(orders);
    }

    public async Task<IEnumerable<OrderDto>> GetBySellerAsync(Guid sellerId)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.Items.Any(i => i.Product.SellerId == sellerId))
            .ToListAsync();

        return _mapper.Map<List<OrderDto>>(orders);
    }
}