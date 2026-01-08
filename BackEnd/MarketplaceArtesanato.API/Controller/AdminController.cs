using AutoMapper;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO; // Ensure this namespace has your DTOs
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Requests; // For UpdateCommissionRateDto, etc.
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        [HttpGet("sellers/pending")]
        public async Task<ActionResult<List<PendingSellerDto>>> GetPendingSellers()
        {
            var sellers = await _adminService.GetPendingSellersAsync();
            return Ok(sellers);
        }

        [HttpGet("customers")]
        public async Task<ActionResult<List<CustomerResponseDto>>> GetCustomers()
        {
            var paidStatuses = new[] { OrderStatus.Confirmed, OrderStatus.Sent, OrderStatus.Delivered };

            var customersData = await _context.Customers
                .AsNoTracking()
                .Include(c => c.User)
                .Where(c => !c.IsDeleted)
                .Select(c => new
                {
                    c.Id,
                    c.User.Name,
                    c.User.Email,
                    c.User.Phone,
                    c.User.CPF,
                    ProfileImageUrl = c.User.ProfileImageUrl ?? "/assets/default-avatar.png",
                    CreatedAt = c.CreatedAt,
                    LastOrderDate = _context.Orders
                        .Where(o => o.BuyerId == c.UserId)
                        .OrderByDescending(o => o.CreatedAt)
                        .Select(o => (DateTime?)o.CreatedAt)
                        .FirstOrDefault(),
                    TotalSpent = _context.Orders
                        .Where(o => o.BuyerId == c.UserId && paidStatuses.Contains(o.Status))
                        .Sum(o => (decimal?)o.TotalAmount) ?? 0m
                })
                .ToListAsync();

            var customerDtos = customersData.Select(c => new CustomerResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                CPF = c.CPF,
                ProfileImageUrl = c.ProfileImageUrl,
                CreatedAt = c.CreatedAt.ToString("O"),
                LastOrderDate = c.LastOrderDate?.ToString("O"),
                TotalSpent = c.TotalSpent
            })
            .OrderByDescending(x => x.TotalSpent)
            .ToList();

            return Ok(customerDtos);
        }

        [HttpGet("commission-report")]
        public async Task<ActionResult<List<CommissionReportItemResponse>>> GetCommissionReport()
        {
            var report = await _adminService.GetCommissionReportAsync();
            return Ok(report);
        }

        [HttpGet("dashboard-stats")]
        public async Task<ActionResult<DashboardStatsResponse>> GetDashboardStats()
        {
            var stats = await _adminService.GetDashboardStatsAsync();
            return Ok(stats);
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
                return NotFound(new { message = "Vendedor não encontrado." });
            }
        }

        [HttpPost("reject-seller/{id}")] 
        public async Task<IActionResult> RejectSeller(Guid id)
        {
            try
            {
                await _adminService.RejectSellerAsync(id);
                return Ok(new { message = "Vendedor rejeitado." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Vendedor não encontrado." });
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
                return NotFound(new { message = "Vendedor não encontrado." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
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
                return BadRequest(new { message = ex.Message });
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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("settings/service-fee")]
        public async Task<IActionResult> GetServiceFee()
        {
            var fee = await _settingsService.GetServiceFeeAsync();
            return Ok(new { fee });
        }

        [HttpGet("settings/commission-rate")]
        public async Task<IActionResult> GetCommissionRate()
        {
            var rate = await _settingsService.GetCommissionRateAsync();
            return Ok(new { rate });
        }

        [HttpGet("sales-by-month")]
        public async Task<ActionResult> GetSalesByMonth([FromQuery] DateTime? start = null, [FromQuery] DateTime? end = null)
        {
            var startDate = start?.Date;
            var endDate = end.HasValue ? end.Value.Date.AddDays(1).AddTicks(-1) : (DateTime?)null;
            // ETAPA 1: Consulta ao Banco de Dados (SQL Puro)
            // Trazemos apenas os dados numéricos (Ano, Mês e Total)
            var rawData = await _context.Orders
                .Where(o => o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Sent || o.Status == OrderStatus.Delivered)
                .Where(o => !startDate.HasValue || o.CreatedAt >= startDate.Value)
                .Where(o => !endDate.HasValue || o.CreatedAt <= endDate.Value)
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(r => r.Year)       // Ordena corretamente por data (número)
                .ThenBy(r => r.Month)
                .ToListAsync(); // Aqui o EF executa a query e traz os dados para a memória

            // ETAPA 2: Formatação em Memória (C#)
            // Agora que os dados estão na memória, podemos usar interpolação de string
            var result = rawData.Select(x => new
            {
                month = $"{x.Month:D2}/{x.Year}", // Formata "05/2024"
                total = x.Total
            });

            return Ok(result);
        }
    }

    // Simple DTO for Customers
    public class CustomerResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string CPF { get; set; }
        public string ProfileImageUrl { get; set; }
        public string CreatedAt { get; set; }
        public string? LastOrderDate { get; set; }
        public decimal TotalSpent { get; set; }
    }
}



