using MarketplaceArtesanato.API.Extensions; // Para User.GetUserId()
using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet]
        public async Task<ActionResult> GetProducts(
            [FromQuery] string? search = null,
            [FromQuery] string? subcategory = null,
            [FromQuery] int? category = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] Guid? sellerId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _productService.GetAllAsync(page, pageSize, search, subcategory, category, minPrice, maxPrice, sellerId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponseDto>> GetProduct(Guid id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpPost]
        [Authorize(Roles = "Seller,Admin")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ProductResponseDto>> CreateProduct([FromForm] CreateProductDto dto)
        {
            Console.WriteLine($"DTO recebido:");
            Console.WriteLine($"Name: {dto.Name}");
            Console.WriteLine($"Images: {dto.Images?.Count ?? 0}");
            Console.WriteLine($"Tags: {dto.Tags?.Count ?? 0}");
            Console.WriteLine($"Category: {dto.Category}");
            Console.WriteLine($"ModelState válido: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Erro em {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                return BadRequest(ModelState);
            }
            if (dto.Images == null || !dto.Images.Any()) return BadRequest("Pelo menos uma imagem é obrigatória.");

            try
            {
                var userId = User.GetUserId();
                var result = await _productService.CreateAsync(userId, dto);

                return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO CREATE PRODUCT] {ex.Message}");
                return StatusCode(500, "Erro interno ao criar produto.");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto)
        {
            if (id != dto.Id) return BadRequest("ID mismatch.");
            if (dto.SalePrice.HasValue)
                return BadRequest("Desconto por produto desativado. Use a aba de Promoções.");

            try
            {
                var userId = User.GetUserId();
                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

                var success = await _productService.UpdateAsync(id, userId, role, dto);

                if (!success) return NotFound();

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO UPDATE PRODUCT] {ex.Message}");
                return StatusCode(500, "Erro interno.");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Seller,Admin")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            try
            {
                var userId = User.GetUserId();
                var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

                var success = await _productService.DeleteAsync(id, userId, role);

                if (!success) return NotFound();

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO DELETE PRODUCT] {ex.Message}");
                return StatusCode(500, "Erro interno.");
            }
        }
    }
}
