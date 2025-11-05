using AutoMapper;
using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartsController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly IMapper _mapper;

        public CartsController(ICartService cartService, IMapper mapper)
        {
            _cartService = cartService;
            _mapper = mapper;
        }

        /// <summary>
        /// Obtém o carrinho do cliente autenticado
        /// </summary>
        [HttpGet]
        [SwaggerResponse(200, "Carrinho retornado", typeof(CartDto))]
        [SwaggerResponse(404, "Carrinho vazio")]
        public async Task<ActionResult<CartDto>> GetCart()
        {
            var customerId = User.GetUserId();
            var cart = await _cartService.GetCartAsync(customerId);

            if (!cart.Items.Any())
                return Ok(new CartDto { CustomerId = customerId, Items = new() });

            return Ok(cart);
        }

        [HttpPost("add")]
        [SwaggerResponse(200, "Produto adicionado")]
        [SwaggerResponse(400, "Dados inválidos ou estoque insuficiente")]
        public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var customerId = User.GetUserId();
            await _cartService.AddItemAsync(customerId, request.ProductId, request.Quantity);

            return Ok(new { message = "Produto adicionado ao carrinho" });
        }

        [HttpPut("update")]
        [SwaggerResponse(200, "Quantidade atualizada")]
        [SwaggerResponse(404, "Item não encontrado")]
        public async Task<IActionResult> UpdateItem([FromBody] UpdateCartItemRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var customerId = User.GetUserId();
            await _cartService.UpdateItemQuantityAsync(customerId, request.ProductId, request.Quantity);

            return Ok(new { message = "Quantidade atualizada" });
        }

        [HttpDelete("remove/{productId}")]
        [SwaggerResponse(200, "Item removido")]
        public async Task<IActionResult> RemoveItem(Guid productId)
        {
            var customerId = User.GetUserId();
            await _cartService.RemoveItemAsync(customerId, productId);

            return Ok(new { message = "Item removido do carrinho" });
        }

        [HttpDelete("clear")]
        [SwaggerResponse(200, "Carrinho limpo")]
        public async Task<IActionResult> ClearCart()
        {
            var customerId = User.GetUserId();
            await _cartService.ClearCartAsync(customerId);

            return Ok(new { message = "Carrinho limpo com sucesso" });
        }
    }
}
