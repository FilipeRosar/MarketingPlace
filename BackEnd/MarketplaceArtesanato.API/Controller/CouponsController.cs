using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CouponsController : ControllerBase
    {
        private readonly ICouponService _couponService;
        private readonly ICouponAnalyticsService _analyticsService;
        private readonly ICouponAutomationService _automationService;
        private readonly ArtesianDbContext _context;

        public CouponsController(
            ICouponService couponService, 
            ICouponAnalyticsService analyticsService,
            ICouponAutomationService automationService,
            ArtesianDbContext context)
        {
            _couponService = couponService;
            _analyticsService = analyticsService;
            _automationService = automationService;
            _context = context;
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCoupon(CreateCouponDto dto)
        {
            try
            {
                var coupon = await _couponService.CreateCouponAsync(dto);
                return CreatedAtAction(nameof(GetCouponById), new { id = coupon.Id }, coupon);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllCoupons([FromQuery] CouponType? type = null, [FromQuery] bool? activeOnly = true)
        {
            try
            {
                var coupons = await _couponService.GetAllCouponsAsync(type, activeOnly);
                return Ok(coupons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCouponById(Guid id)
        {
            try
            {
                var coupon = await _couponService.GetCouponByIdAsync(id);
                if (coupon == null)
                    return NotFound();

                return Ok(coupon);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCoupon(Guid id, UpdateCouponDto dto)
        {
            try
            {
                var coupon = await _couponService.UpdateCouponAsync(id, dto);
                return Ok(coupon);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCoupon(Guid id)
        {
            try
            {
                await _couponService.DeleteCouponAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id}/usage")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetCouponUsage(Guid id)
        {
            try
            {
                var usage = await _couponService.GetCouponUsageAsync(id);
                return Ok(usage);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("platform/list")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPlatformCoupons()
        {
            try
            {
                var coupons = await _couponService.GetPlatformCouponsAsync();
                return Ok(coupons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost("seller")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CreateSellerCoupon(CreateCouponDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // Validar que o seller está criando cupom para sua própria loja
                if (!Guid.TryParse(userId, out var sellerId))
                    return Unauthorized();

                // Validar plano do seller
                var seller = await _context.Sellers
                    .Include(s => s.Subscription)
                    .FirstOrDefaultAsync(s => s.UserId == sellerId);

                if (seller == null)
                    return NotFound(new { message = "Seller não encontrado" });

                var currentPlan = seller.Subscription?.Plan ?? SellerPlan.Basic;

                // Basic (Free) não pode criar cupons
                if (currentPlan == SellerPlan.Basic)
                    return Forbid("Plano Basic não permite criar cupons. Faça upgrade para Pro ou Premium.");

                // Pro permite até 3 cupons ativos
                if (currentPlan == SellerPlan.Pro)
                {
                    var activeCouponCount = await _context.Coupons
                        .Where(c => c.CreatorSellerId == sellerId && !c.IsDeleted && c.IsActive)
                        .CountAsync();

                    if (activeCouponCount >= 3)
                        return BadRequest(new { message = "Plano Pro permite apenas 3 cupons ativos. Delete um cupom antes de criar novo." });
                }

                dto.Type = CouponType.Seller;
                dto.CreatorSellerId = sellerId;

                var coupon = await _couponService.CreateCouponAsync(dto);
                return CreatedAtAction(nameof(GetCouponById), new { id = coupon.Id }, coupon);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter cupons do seller
        /// </summary>
        [HttpGet("seller/my-coupons")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetMySellerCoupons()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var sellerId))
                    return Unauthorized();

                var coupons = await _couponService.GetSellerCouponsAsync(sellerId);
                return Ok(coupons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Atualizar cupom do seller
        /// </summary>
        [HttpPut("seller/{id}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateSellerCoupon(Guid id, UpdateCouponDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var sellerId))
                    return Unauthorized();

                var coupon = await _couponService.GetCouponByIdAsync(id);
                if (coupon == null)
                    return NotFound();

                // Validar que o seller é o dono
                if (coupon.CreatorSellerId != sellerId)
                    return Forbid();

                var updated = await _couponService.UpdateCouponAsync(id, dto);
                return Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Deletar cupom do seller
        /// </summary>
        [HttpDelete("seller/{id}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> DeleteSellerCoupon(Guid id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var sellerId))
                    return Unauthorized();

                var coupon = await _couponService.GetCouponByIdAsync(id);
                if (coupon == null)
                    return NotFound();

                // Validar que o seller é o dono
                if (coupon.CreatorSellerId != sellerId)
                    return Forbid();

                await _couponService.DeleteCouponAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ================== PUBLIC ENDPOINTS ==================

        /// <summary>
        /// Validar e obter informações de cupom
        /// </summary>
        [HttpPost("validate")]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateCoupon(ValidateCouponRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Guid.NewGuid().ToString();
                if (!Guid.TryParse(userId, out var userGuid))
                    userGuid = Guid.NewGuid();

                var result = await _couponService.ValidateCouponAsync(
                    request.CouponCode,
                    userGuid,
                    request.OrderTotal,
                    request.ProductIds ?? new List<Guid>(),
                    request.SellerId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Aplicar cupom a um pedido
        /// </summary>
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyCoupon(ApplyCouponRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
                    return Unauthorized();

                var result = await _couponService.ApplyCouponAsync(
                    request.OrderId,
                    request.CouponCode,
                    userGuid,
                    request.OrderTotal,
                    request.ProductIds ?? new List<Guid>(),
                    request.SellerId);

                if (!result.IsValid)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter cupons ativos disponíveis
        /// </summary>
        [HttpGet("active/list")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveCoupons()
        {
            try
            {
                var coupons = await _couponService.GetActiveCouponsAsync();
                return Ok(coupons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        [HttpGet("seller/stats/{id}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetSellerCouponStats(Guid id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var sellerId))
                    return Unauthorized();

                var coupon = await _couponService.GetCouponByIdAsync(id);
                if (coupon == null)
                    return NotFound();

                // Validar que o seller é o dono do cupom
                if (coupon.CreatorSellerId != sellerId)
                    return Forbid();

                var usages = await _context.CouponUsages
                    .Where(cu => cu.CouponId == id && !cu.IsDeleted)
                    .ToListAsync();

                var stats = new
                {
                    coupon.Id,
                    coupon.Code,
                    coupon.Description,
                    coupon.IsActive,
                    coupon.ValidFrom,
                    coupon.ValidUntil,
                    TotalUses = usages.Count,
                    TotalDiscountConceded = usages.Sum(u => u.DiscountApplied),
                    UniqueCustomers = usages.Select(u => u.UserId).Distinct().Count(),
                    AverageDiscount = usages.Count > 0 ? usages.Average(u => u.DiscountApplied) : 0,
                    UsageLimitReached = coupon.UsageLimit > 0 && usages.Count >= coupon.UsageLimit
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter overview de análise de cupons do seller
        /// </summary>
        [HttpGet("seller/analytics/overview")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetSellerCouponsAnalyticsOverview()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var sellerId))
                    return Unauthorized();

                var coupons = await _context.Coupons
                    .Where(c => c.CreatorSellerId == sellerId && !c.IsDeleted)
                    .ToListAsync();

                var couponIds = coupons.Select(c => c.Id).ToList();
                var usages = await _context.CouponUsages
                    .Where(cu => couponIds.Contains(cu.CouponId) && !cu.IsDeleted)
                    .ToListAsync();

                var analytics = new
                {
                    TotalCoupons = coupons.Count,
                    ActiveCoupons = coupons.Count(c => c.IsActive),
                    TotalUses = usages.Count,
                    TotalDiscountConceded = usages.Sum(u => u.DiscountApplied),
                    UniqueCustomers = usages.Select(u => u.UserId).Distinct().Count(),
                    AverageDiscountPerCoupon = coupons.Count > 0 ? usages.Sum(u => u.DiscountApplied) / coupons.Count : 0,
                    MostUsedCoupon = coupons
                        .Select(c => new
                        {
                            c.Code,
                            Uses = usages.Count(u => u.CouponId == c.Id)
                        })
                        .OrderByDescending(x => x.Uses)
                        .FirstOrDefault()
                };

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ================== ADVANCED ANALYTICS ENDPOINTS ==================

        /// <summary>
        /// Obter ROI detalhado de um cupom específico
        /// </summary>
        [HttpGet("seller/{sellerId}/coupons/{couponId}/analytics")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetCouponROI(Guid sellerId, Guid couponId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var currentUserId))
                    return Unauthorized();

                // Validar que o seller é o dono
                if (currentUserId != sellerId)
                    return Forbid();

                var roi = await _analyticsService.CalculateROIAsync(couponId);
                return Ok(roi);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter estatísticas gerais de cupons de um seller
        /// </summary>
        [HttpGet("seller/{sellerId}/stats")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetSellerGeneralStats(Guid sellerId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var currentUserId))
                    return Unauthorized();

                if (currentUserId != sellerId)
                    return Forbid();

                var stats = await _analyticsService.GetSellerCouponStatsAsync(sellerId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter performance de cupom em período específico
        /// </summary>
        [HttpGet("seller/{sellerId}/coupons/{couponId}/performance")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetCouponPerformance(Guid sellerId, Guid couponId, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var currentUserId))
                    return Unauthorized();

                if (currentUserId != sellerId)
                    return Forbid();

                if (endDate < startDate)
                    return BadRequest(new { message = "Data final deve ser após data inicial" });

                var performance = await _analyticsService.GetCouponPerformanceAsync(couponId, startDate, endDate);
                return Ok(performance);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter comparação de performance entre cupons do seller
        /// </summary>
        [HttpGet("seller/{sellerId}/coupons/comparison")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetCouponsComparison(Guid sellerId, [FromQuery] int topN = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var currentUserId))
                    return Unauthorized();

                if (currentUserId != sellerId)
                    return Forbid();

                var comparison = await _analyticsService.GetSellerCouponsComparisonAsync(sellerId, topN);
                return Ok(comparison);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter dashboard completo de analytics de cupons
        /// </summary>
        [HttpGet("seller/{sellerId}/dashboard")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetAnalyticsDashboard(Guid sellerId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var currentUserId))
                    return Unauthorized();

                if (currentUserId != sellerId)
                    return Forbid();

                var dashboard = await _analyticsService.GetCouponAnalyticsDashboardAsync(sellerId);
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ================== AUTOMATION ENDPOINTS ==================

        /// <summary>
        /// Executar automações de cupons manualmente (Admin only)
        /// </summary>
        [HttpPost("automation/execute")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ExecuteAutomations()
        {
            try
            {
                await _automationService.ExecuteAllAutomationsAsync();
                return Ok(new { message = "Automações de cupons executadas com sucesso" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter histórico de logs de automação
        /// </summary>
        [HttpGet("automation/logs")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAutomationLogs([FromQuery] int days = 7)
        {
            try
            {
                var logs = await _automationService.GetAutomationLogsAsync(days);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
