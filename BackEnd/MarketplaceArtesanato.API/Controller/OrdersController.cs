using AutoMapper;
using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/orders")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ArtesianDbContext _context;
        private readonly IStripePaymentService _stripeService;
        private readonly IMapper _mapper;

        public OrdersController(
            ArtesianDbContext context,
            IStripePaymentService stripeService,
            IMapper mapper)
        {
            _context = context;
            _stripeService = stripeService;
            _mapper = mapper;
        }

        [HttpGet("my-orders")]
        [Authorize]
        public async Task<ActionResult<List<OrderResponseDto>>> GetMyOrders()
        {
            var userId = User.GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";

            var query = _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .AsNoTracking();

            if (role == "Seller")
            {
                query = query.Where(o => o.Items.Any(i => i.Product.SellerId == userId));
            }
            else if (role == "Admin")
            {
            }
            else
            {
                query = query.Where(o => o.BuyerId == userId);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<OrderResponseDto>>(orders);
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<OrderResponseDto>> GetOrder(Guid id)
        {
            var userId = User.GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Images)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                        .ThenInclude(p => p.Seller)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            bool isBuyer = order.BuyerId == userId;
            bool isSeller = role == "Seller" && order.Items.Any(i => i.Product.SellerId == userId);
            bool isAdmin = User.IsInRole("Admin");

            if (!isBuyer && !isSeller && !isAdmin) return Forbid();

            var dto = _mapper.Map<OrderResponseDto>(order);
            return Ok(dto);
        }

        [HttpPost("checkout")]
        [Authorize]
        public async Task<ActionResult> Checkout([FromBody] CheckoutRequestDto dto)
        {
            var buyerId = User.GetUserId();
            var buyer = await _context.Users.FindAsync(buyerId);
            if (buyer == null) return NotFound("Usuário não encontrado.");

            var productIds = dto.Items.Select(i => i.ProductId).ToList();
            var products = await _context.Products
                .Include(p => p.Seller)
                .Where(p => productIds.Contains(p.Id))
                .ToListAsync();

            if (products.Count != productIds.Count)
                return BadRequest("Algum produto não encontrado ou indisponível.");

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

                if (product.StockQuantity < itemDto.Quantity)
                    return BadRequest($"Estoque insuficiente para {product.Name}");

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


                var sellerId = product.SellerId;
                var itemTotal = orderItem.UnitPrice * orderItem.Quantity;
                var commission = itemTotal * (product.Seller.CommissionRate / 100m);

                if (order.SellerCommissions.ContainsKey(sellerId))
                    order.SellerCommissions[sellerId] += commission;
                else
                    order.SellerCommissions[sellerId] = commission;
            }

            order.TotalAmount = orderTotal;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var paymentUrl = await _stripeService.CreateCheckoutSessionAsync(order, buyerId);

            return Ok(new
            {
                message = "Pedido criado com sucesso! Redirecionando para pagamento...",
                orderId = order.Id,
                paymentUrl
            });
        }

        [HttpPut("{id}/tracking")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UpdateTracking(Guid id, [FromBody] UpdateTrackingDto dto)
        {
            var userId = User.GetUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Check if the seller owns any item in this order
            if (role == "Seller")
            {
                bool isMyOrder = order.Items.Any(i => i.Product.SellerId == userId);
                if (!isMyOrder) return Forbid("This order does not contain your products.");

                // Update the dictionary for this specific seller
                if (order.TrackingCodes == null) order.TrackingCodes = new Dictionary<Guid, string>();
                order.TrackingCodes[userId] = dto.TrackingCode;
            }
            else if (role == "Admin")
            {
                // Admin logic...
                var firstSeller = order.Items.First().Product.SellerId;
                if (order.TrackingCodes == null) order.TrackingCodes = new Dictionary<Guid, string>();
                order.TrackingCodes[firstSeller] = dto.TrackingCode;
            }

            // Update status logic...
            if (order.Status == OrderStatus.Paid)
            {
                order.Status = OrderStatus.Sent;
            }

            // Important: Re-assign to ensure EF Core detects the change in the dictionary
            order.TrackingCodes = new Dictionary<Guid, string>(order.TrackingCodes);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Tracking updated." });
        }
    }
}