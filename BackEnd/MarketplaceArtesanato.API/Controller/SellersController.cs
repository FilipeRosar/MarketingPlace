using AutoMapper;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/sellers")]
    [ApiController]
    public class SellersController : ControllerBase
    {
        private readonly ArtesianDbContext _context;
        private readonly IMapper _mapper;

        public SellersController(ArtesianDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SellerResponseDto>> GetSeller(Guid id)
        {
            try
            {
                var seller = await _context.Sellers
                    .Include(s => s.Address)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (seller == null) return NotFound("Vendedor não encontrado no banco de dados.");

                if (seller.Address == null)
                {
                    Console.WriteLine($"[AVISO] Vendedor {seller.Name} está sem endereço vinculado.");
                }

                var dto = _mapper.Map<SellerResponseDto>(seller);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO FATAL] Falha ao buscar vendedor: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"[INNER] {ex.InnerException.Message}");

                return StatusCode(500, new { message = "Erro interno ao buscar vendedor.", details = ex.Message });
            }
        }
    }
}