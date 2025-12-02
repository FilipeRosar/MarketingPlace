using AutoMapper;
using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/carts")] 
    [ApiController]
    [Authorize] 
    public class CartsController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IMapper _mapper;

        public CartsController(ICartService cartService, IMapper mapper)
        {
            _cartService = cartService;
            _mapper = mapper;
        }

        [HttpGet]
        [SwaggerResponse(200, "Carrinho retornado", typeof(CartDto))]
        public async Task<ActionResult<CartDto>> GetCart()
        {
            var customerId = User.GetUserId();
            var cart = await _cartService.GetCartAsync(customerId);

            if (cart == null)
                return Ok(new CartDto { CustomerId = customerId, Items = new() });

            return Ok(cart);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var customerId = User.GetUserId();
            try
            {
                await _cartService.AddItemAsync(customerId, request.ProductId, request.Quantity);
                return Ok(new { message = "Produto adicionado ao carrinho" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message }); 
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var customerId = User.GetUserId();
            try
            {
                await _cartService.UpdateItemQuantityAsync(customerId, request.ProductId, request.Quantity);
                return Ok(new { message = "Quantidade atualizada" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Item não encontrado no carrinho" });
            }
        }

        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveItem(Guid productId)
        {
            var customerId = User.GetUserId();
            await _cartService.RemoveItemAsync(customerId, productId);
            return Ok(new { message = "Item removido do carrinho" });
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var customerId = User.GetUserId();
            await _cartService.ClearCartAsync(customerId);
            return Ok(new { message = "Carrinho limpo com sucesso" });
        }
    }
}