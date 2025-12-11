using AutoMapper;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ArtesianDbContext _context;
        private readonly IAdminService _adminService;
        private readonly ISettingsService _settingsService;

        public AdminController(ArtesianDbContext context, IAdminService adminService, ISettingsService settingsService)
        {
            _adminService = adminService;
            _context = context;
            _settingsService = settingsService;
        }

        [HttpGet("pending-sellers")]
        public async Task<ActionResult> GetPendingSellers()
        {
            var sellers = await _adminService.GetPendingSellersAsync();
            return Ok(sellers);
        }

        [HttpPost("approve-seller/{id}")]
        public async Task<IActionResult> ApproveSeller(Guid id)
        {
            try
            {
                await _adminService.ApproveSellerAsync(id);
                return Ok(new { message = "Vendedor aprovado com sucesso!" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Vendedor não encontrado.");
            }
        }

        [HttpPost("reject-seller/{sellerId}")]
        public async Task<IActionResult> RejectSeller(Guid sellerId)
        {
            try
            {
                await _adminService.RejectSellerAsync(sellerId);
                return Ok(new { message = "Vendedor rejeitado." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Vendedor não encontrado.");
            }
        }

        [HttpPut("sellers/{id}/commission")]
        public async Task<IActionResult> SetCommission(Guid id, [FromBody] decimal? rate)
        {
            try
            {
                await _adminService.SetSellerCommissionAsync(id, rate);
                return Ok(new { message = "Taxa de comissão atualizada com sucesso." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Vendedor não encontrado.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut("commission-rate")]
        public async Task<IActionResult> UpdateCommissionRate([FromBody] UpdateCommissionRateDto dto)
        {
            try
            {
                await _adminService.UpdateCommissionRateAsync(dto.Rate);
                return Ok(new { message = "Taxa de comissão da plataforma atualizada!" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("service-fee")]
        public async Task<IActionResult> UpdateServiceFee([FromBody] UpdateServiceFeeDto dto)
        {
            try
            {
                await _adminService.UpdateServiceFeeAsync(dto.Fee);
                return Ok(new { message = "Taxa de serviço atualizada!" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("settings/service-fee")]
        public async Task<IActionResult> GetServiceFee()
        {
            var fee = await _settingsService.GetServiceFeeAsync();
            return Ok(fee);
        }
    }
}