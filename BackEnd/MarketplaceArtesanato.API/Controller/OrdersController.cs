using AutoMapper;
using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
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
        private readonly StripePaymentService _stripeService;
        private readonly IMapper _mapper;

        public OrdersController(
            ArtesianDbContext context,
            StripePaymentService stripeService,
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
            try
            {
                var userIdClaim = User.FindFirst("sub")
                               ?? User.FindFirst("nameid")
                               ?? User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized("Token inválido ou usuário não encontrado");
                }

                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Customer";

                var query = _context.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                            .ThenInclude(p => p.Images)
                    .AsNoTracking();

                if (role == "Seller" || role == "Admin")
                {
                    query = query.Where(o => o.Items.Any(i => i.Product.SellerId == userId));
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
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO MY-ORDERS] {ex.Message}");
                return StatusCode(500, "Erro interno do servidor");
            }
        }
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<OrderResponseDto>> GetOrder(Guid id)
        {
            try
            {
                var userId = User.GetUserId();
                var role = User.FindFirst(ClaimTypes.Role)?.Value;

                var order = await _context.Orders
                    .Include(o => o.Items)
                        .ThenInclude(i => i.Product)
                            .ThenInclude(p => p.Images)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null) return NotFound();

                bool isBuyer = order.BuyerId == userId;
                bool isSeller = role == "Seller" && order.Items.Any(i => i.Product != null && i.Product.SellerId == userId);
                bool isAdmin = User.IsInRole("Admin");

                if (!isBuyer && !isSeller && !isAdmin) return Forbid();

                var dto = _mapper.Map<OrderResponseDto>(order);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO GET ORDER DETALHE] {ex.Message}");
                return StatusCode(500, "Erro ao carregar detalhes do pedido.");
            }
        }

        [HttpPost("checkout")]
        [Authorize(Roles = "Customer")]
        public async Task<ActionResult> Checkout([FromBody] CheckoutRequestDto dto)
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdString, out var customerId))
                return Unauthorized("Usuário inválido.");

            var order = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = customerId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                Items = new List<OrderItem>()
            };

            decimal totalAmount = 0;

            foreach (var itemDto in dto.Items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);

                if (product == null)
                    return BadRequest(new { message = $"Produto {itemDto.ProductId} não encontrado" });

                if (product.StockQuantity < itemDto.Quantity)
                    return BadRequest(new { message = $"Estoque insuficiente para {product.Name}" });

                totalAmount += product.Price * itemDto.Quantity;

                order.Items.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,
                    OrderId = order.Id
                });
            }

            order.TotalAmount = totalAmount;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return Ok(new { order.Id, order.TotalAmount, status = order.Status.ToString() });
        }

        // --- PUT: RASTREIO ---
        [HttpPut("{id}/tracking")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UpdateTracking(Guid id, [FromBody] UpdateTrackingDto dto)
        {
            var userId = User.GetUserId();
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            if (role == "Seller")
            {
                bool isMyOrder = order.Items.Any(i => i.Product != null && i.Product.SellerId == userId);
                if (!isMyOrder) return Forbid("Este pedido não contém seus produtos.");
            }

            order.TrackingCode = dto.TrackingCode;
            order.Carrier = dto.Carrier;
            order.ShippedAt = DateTime.UtcNow;

            if (order.Status == OrderStatus.Paid)
            {
                order.Status = OrderStatus.Sent;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Rastreio atualizado com sucesso." });
        }
    }
}