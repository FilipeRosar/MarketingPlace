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
            Items = dto.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = _context.Products.First(p => p.Id == i.ProductId).Price
            }).ToList(),
            Total = dto.Items.Sum(i => i.Quantity * _context.Products.First(p => p.Id == i.ProductId).Price),
            Status = OrderStatus.Pending
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var sessionUrl = await _stripeService.CreateCheckoutSessionAsync(order);
        return Ok(new { url = sessionUrl });
    }
}