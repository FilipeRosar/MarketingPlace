using AutoMapper;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Events;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
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

        var productIds = cart.Items.Select(i => i.ProductId).ToList();

        var products = await _context.Products
            .Include(p => p.Seller) 
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        var orderItems = new List<OrderItem>();
        var commissions = new Dictionary<Guid, decimal>();

        foreach (var cartItem in cart.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == cartItem.ProductId);

            if (product == null)
                throw new InvalidOperationException($"Produto {cartItem.ProductName} não encontrado.");

            if (product.StockQuantity < cartItem.Quantity)
                throw new InvalidOperationException($"Estoque insuficiente para {product.Name}. Disponível: {product.StockQuantity}");

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Quantity = cartItem.Quantity,
                UnitPrice = product.Price, 
                ProductName = product.Name,
                ProductImage = product.Images.FirstOrDefault()?.Url 
            };

            orderItems.Add(orderItem);

            var subtotal = orderItem.UnitPrice * orderItem.Quantity;

            var rate = product.Seller.CommissionRate > 0 ? product.Seller.CommissionRate : 12.0m;
            var commissionValue = subtotal * (rate / 100m);

            if (commissions.ContainsKey(product.SellerId))
                commissions[product.SellerId] += commissionValue;
            else
                commissions[product.SellerId] = commissionValue;
        }

        var order = new Order
        {
            BuyerId = customerId,
            Items = orderItems,
            SellerCommissions = commissions,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        order.CalculateTotal();

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        await _cartService.ClearCartAsync(customerId);

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<bool> ProcessPaymentAsync(PaymentProcessedEvent evt)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Buyer)
            .FirstOrDefaultAsync(o => o.Id == evt.OrderId);

        if (order != null && order.Status == OrderStatus.Paid) return true;

        if (order == null || order.Status != OrderStatus.Pending)
            return false;

        order.Status = OrderStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;
        order.StripeSessionId = evt.StripeSessionId;
        order.StripePaymentIntentId = evt.PaymentIntentId;

        foreach (var item in order.Items)
        {
            if (item.Product.StockQuantity >= item.Quantity)
            {
                item.Product.StockQuantity -= item.Quantity;
            }
            else
            {
                item.Product.StockQuantity = 0;
            }
        }

        await _context.SaveChangesAsync();

        await _publishEndpoint.Publish(new OrderPaidEvent
        {
            OrderId = order.Id,
            Total = order.TotalAmount,
            SellerCommissions = order.SellerCommissions,

            BuyerEmail = order.Buyer.Email, 
            BuyerName = order.Buyer.Name    
        });

        return true;
    }

    public async Task<OrderDto?> GetByIdAsync(Guid orderId, Guid userId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Buyer) 
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return null;

        var isCustomer = order.BuyerId == userId;
        var isSeller = order.Items.Any(i => i.SellerId == userId);
   
        // .Include(o => o.Items).ThenInclude(i => i.Product)

        if (!isCustomer && !isSeller  )
            throw new UnauthorizedAccessException("Acesso negado ao pedido.");

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<IEnumerable<OrderDto>> GetByCustomerAsync(Guid customerId)
    {
        var orders = await _context.Orders
            .Where(o => o.BuyerId == customerId)
            .Include(o => o.Items) 
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<OrderDto>>(orders);
    }

    public async Task<IEnumerable<OrderDto>> GetBySellerAsync(Guid sellerId)
    {
        var orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.Items.Any(i => i.Product.SellerId == sellerId))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return _mapper.Map<List<OrderDto>>(orders);
    }
}