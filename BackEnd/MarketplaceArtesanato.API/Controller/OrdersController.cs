using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[Route("api/orders")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly ArtesianDbContext _context;
    private readonly StripePaymentService _stripeService;

    public OrdersController(ArtesianDbContext context, StripePaymentService stripeService)
    {
        _context = context;
        _stripeService = stripeService;
    }

    [HttpPost("checkout")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult> Checkout([FromBody] CheckoutRequestDto dto)
    {
        var customerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            BuyerId = customerId,
            Status = OrderStatus.Pending,
            Items = new List<OrderItem>()
        };

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.FindAsync(itemDto.ProductId)
                ?? throw new KeyNotFoundException($"Produto {itemDto.ProductId} não encontrado");

            if (product.StockQuantity < itemDto.Quantity)
                throw new InvalidOperationException($"Estoque insuficiente para {product.Name}");

            order.Items.Add(new OrderItem
            {
                ProductId = itemDto.ProductId,
                Quantity = itemDto.Quantity,
                UnitPrice = product.Price
            });
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(new { order.Id, order.TotalAmount, order.Status });
    }
}