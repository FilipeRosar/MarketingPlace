using MarketplaceArtesanato.API.Extensions;
using MarketplaceArtesanato.API.Models.Requests;
using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Models.Requests;
using MarketplaceArtesanato.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

namespace MarketplaceArtesanato.API.Controllers
{
    [Route("api/sellers")]
    [ApiController]
    public class SellersController : ControllerBase
    {
        private readonly ISellerService _sellerService;
        private readonly IStorageService _storageService;
        private readonly IStripeConnectService _stripeConnectService;
        private readonly ISellerSubscriptionService _sellerSubscriptionService;
        private readonly ISellerAnalyticsService _sellerAnalyticsService;

        public SellersController(ISellerService sellerService, IStorageService storageService, IStripeConnectService stripeConnectService, ISellerSubscriptionService sellerSubscriptionService, ISellerAnalyticsService sellerAnalyticsService)
        {
            _sellerService = sellerService;
            _storageService = storageService;
            _stripeConnectService = stripeConnectService;
            _sellerSubscriptionService = sellerSubscriptionService;
            _sellerAnalyticsService = sellerAnalyticsService;

        }

        [HttpGet]
        public async Task<ActionResult<List<SellerResponseDto>>> SearchSellers([FromQuery] string? search = null, [FromQuery] int limit = 6)
        {
            if (string.IsNullOrWhiteSpace(search))
                return Ok(new List<SellerResponseDto>());

            if (limit <= 0) limit = 6;

            var sellers = await _sellerService.SearchAsync(search, limit);
            return Ok(sellers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SellerResponseDto>> GetSeller(Guid id)
        {
            var seller = await _sellerService.GetByIdAsync(id);

            if (seller == null)
                return NotFound("Vendedor não encontrado no banco de dados.");

            return Ok(seller);
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<SellerResponseDto>> GetSellerByUserId(Guid userId)
        {
            var seller = await _sellerService.GetByUserIdAsync(userId);

            if (seller == null)
                return NotFound("Vendedor não encontrado para este usuário.");

            return Ok(seller);
        }

        [HttpPost("{sellerId}/moments/upload-video")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<object>> UploadMomentVideo(Guid sellerId, IFormFile video)
        {
            if (video == null || video.Length == 0)
                return BadRequest("Vídeo é obrigatório.");
            var userId = User.GetUserId();
            if (!await _sellerService.IsOwnerAsync(sellerId, userId))
                return Forbid();
            try
            {
                var videoUrl = await _storageService.UploadVideoAsync(video);
                return Ok(new { videoUrl });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Erro ao fazer upload do vídeo.");
            }
        }

        // POST: api/sellers/{sellerId}/moments/upload-thumb
        [HttpPost("{sellerId}/moments/upload-thumb")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<object>> UploadMomentThumb(Guid sellerId, IFormFile thumb)
        {
            if (thumb == null || thumb.Length == 0)
                return Ok(new { imageUrl = "" }); 

            try
            {
                var imageUrl = await _storageService.UploadFileAsync(thumb); 
                return Ok(new { imageUrl });
            }
            catch (Exception)
            {
                return StatusCode(500, "Erro ao fazer upload da thumbnail.");
            }
        }

        // POST: api/sellers/{sellerId}/moments → Cria o momento
        [HttpPost("{sellerId}/moments")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<MomentResponseDto>> CreateMoment(Guid sellerId, [FromBody] CreateMomentDto dto)
        {
            var currentUserId = User.GetUserId(); 

            var seller = await _sellerService.GetByIdAsync(sellerId);
            var isOwner = await _sellerService.IsOwnerAsync(sellerId, currentUserId);
            if (!isOwner)
                return Forbid("Você não tem permissão para publicar momentos nesta loja.");

            var createdMoment = await _sellerService.CreateMomentAsync(sellerId, dto);

            return CreatedAtAction(nameof(GetMoments), new { sellerId }, createdMoment);
        }

        [HttpGet("{sellerId}/moments")]
        public async Task<ActionResult<List<MomentResponseDto>>> GetMoments(Guid sellerId)
        {
            var moments = await _sellerService.GetMomentsAsync(sellerId);
            return Ok(moments);
        }

        [HttpGet("dashboard")] 
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<SellerDashboardDto>> GetDashboard()
        {
            
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized("Token inválido ou sem ID de usuário.");

                var userId = Guid.Parse(userIdClaim);
                var dashboard = await _sellerService.GetDashboardAsync(userId);

                if (dashboard == null)
                    return NotFound("Perfil de vendedor não encontrado para este usuário.");

               return Ok(dashboard);
            
        }
        [HttpGet("sales")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<List<SellerSaleResponseDto>>> GetSales()
        {
            var userId = User.GetUserId();

            if (userId == Guid.Empty)
                return Unauthorized("Usuário não autenticado.");

            var sales = await _sellerService.GetSalesAsync(userId);

            if (!sales.Any())
                return Ok(new List<SellerSaleResponseDto>()); 

            return Ok(sales);
        }

        [HttpPut("profile")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateSellerProfileDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = User.GetUserId();
            if (userId == Guid.Empty)
                return Unauthorized("Usuário não autenticado.");

            var updated = await _sellerService.UpdateProfileAsync(userId, dto);
            if (!updated)
                return NotFound("Perfil de vendedor não encontrado.");

            return NoContent();
        }

        [HttpPost("stripe/connect")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CreateStripeConnectLink()
        {
            try
            {
                var userId = User.GetUserId();
                var url = await _stripeConnectService.CreateOnboardingLinkAsync(userId);
                return Ok(new { url });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("stripe/status")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<StripeConnectStatusDto>> GetStripeStatus()
        {
            try
            {
                var userId = User.GetUserId();
                var status = await _stripeConnectService.GetStatusAsync(userId);
                return Ok(status);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("stripe/dashboard")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetStripeDashboardLink()
        {
            try
            {
                var userId = User.GetUserId();
                var url = await _stripeConnectService.CreateDashboardLinkAsync(userId);
                return Ok(new { url });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost("subscription/checkout")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CreateSubscriptionCheckout([FromBody] SubscribeRequestDto dto)
        {
            var userId = User.GetUserId();

            var seller = await _sellerService.GetByUserIdAsync(userId);
            if (seller == null)
                return NotFound("Perfil de vendedor nao encontrado para este usuario.");
            try
            {
                if (dto.Plan == SellerPlan.Basic)
                {
                    var subscription = await _sellerSubscriptionService.SubscribeAsync(seller.Id, SellerPlan.Basic);
                    return Ok(new { subscription });
                }

                var url = await _sellerSubscriptionService.CreateCheckoutSessionAsync(seller.Id, dto.Plan);
                return Ok(new { url });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("subscription")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<SellerSubscription>> GetMySubscription()
        {
            var userId = User.GetUserId();

            var seller = await _sellerService.GetByUserIdAsync(userId);
            if (seller == null)
                return NotFound("Perfil de vendedor não encontrado para este usuário.");

            var subscription = await _sellerSubscriptionService.GetActiveSubscriptionAsync(seller.Id);

            if (subscription == null)
                return Ok(null);

            return Ok(subscription);
        }
        [HttpPost("subscription")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<SellerSubscription>> Subscribe([FromBody] SubscribeRequestDto dto)
        {
            var userId = User.GetUserId();
            var seller = await _sellerService.GetByUserIdAsync(userId);
            if (seller == null)
                return NotFound("Perfil de vendedor não encontrado para este usuário.");
            try
            {
                var subscription = await _sellerSubscriptionService.SubscribeAsync(seller.Id, dto.Plan);
                return Ok(subscription);
                
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPut("subscription")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<SellerSubscription>> ChangeSubscriptionPlan([FromBody] SubscribeRequestDto dto)
        {
            var userId = User.GetUserId();
            var seller = await _sellerService.GetByUserIdAsync(userId);
            if (seller == null)
                return NotFound("Vendedor não encontrado.");
            try
            {
                var updatedSubscription = await _sellerSubscriptionService.ChangePlanAsync(seller.Id, dto.Plan);
                return Ok(updatedSubscription);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("subscription")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CancelSubscription()
        {
            var userId = User.GetUserId();

            var seller = await _sellerService.GetByUserIdAsync(userId);
            if (seller == null)
                return NotFound("Vendedor não encontrado.");

            await _sellerSubscriptionService.CancelAsync(seller.Id);

            await _sellerSubscriptionService.SubscribeAsync(seller.Id, SellerPlan.Basic);

            return NoContent();
        }

        #region Advanced Analytics (Pro + Premium)

        [HttpGet("analytics/advanced")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetAdvancedAnalytics()
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var analytics = await _sellerAnalyticsService.GetAdvancedAnalyticsAsync(seller.Id);
                return Ok(analytics);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("analytics/period-comparison")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetPeriodComparison([FromQuery] int days = 30)
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var comparison = await _sellerAnalyticsService.GetPeriodComparisonAsync(seller.Id, days);
                return Ok(comparison);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("analytics/customer-analysis")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetCustomerAnalysis()
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var analysis = await _sellerAnalyticsService.GetCustomerAnalysisAsync(seller.Id);
                return Ok(analysis);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("analytics/products")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetProductPerformance()
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var products = await _sellerAnalyticsService.GetProductPerformanceAsync(seller.Id);
                return Ok(products);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("analytics/trends")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetTrends([FromQuery] int days = 90)
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var trends = await _sellerAnalyticsService.GetTrendsAsync(seller.Id, days);
                return Ok(trends);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("analytics/hourly-revenue")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetHourlyRevenueDistribution()
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var hourly = await _sellerAnalyticsService.GetHourlyRevenueDistributionAsync(seller.Id);
                return Ok(hourly);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("analytics/coupons")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetCouponEffectiveness()
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var coupons = await _sellerAnalyticsService.GetCouponEffectivenessAsync(seller.Id);
                return Ok(coupons);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("analytics/insights")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetAIInsights()
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var insights = await _sellerAnalyticsService.GetAIInsightsAsync(seller.Id);
                return Ok(insights);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("analytics/forecast")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetRevenueForecast([FromQuery] int daysAhead = 30)
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var forecast = await _sellerAnalyticsService.GetRevenueForecastAsync(seller.Id, daysAhead);
                return Ok(forecast);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("analytics/segmentation")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetCustomerSegmentation()
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var segmentation = await _sellerAnalyticsService.GetCustomerSegmentationAsync(seller.Id);
                return Ok(segmentation);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("analytics/seasonal")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetSeasonalAnalysis()
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var seasonal = await _sellerAnalyticsService.GetSeasonalAnalysisAsync(seller.Id);
                return Ok(seasonal);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("analytics/export/csv")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> ExportAnalyticsCSV()
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var csvData = await _sellerAnalyticsService.ExportAnalyticsAsCSVAsync(seller.Id);
                return File(csvData, "text/csv", "analytics.csv");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("analytics/export/pdf")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> ExportAnalyticsPDF()
        {
            try
            {
                var userId = User.GetUserId();
                var seller = await _sellerService.GetByUserIdAsync(userId);
                if (seller == null)
                    return NotFound("Vendedor não encontrado.");

                var pdfData = await _sellerAnalyticsService.ExportAnalyticsAsPDFAsync(seller.Id);
                return File(pdfData, "application/pdf", "analytics.pdf");
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        #endregion
    }
}
