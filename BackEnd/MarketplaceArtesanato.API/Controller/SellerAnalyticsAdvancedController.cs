using MarketplaceArtesanato.API.Authorization;
using MarketplaceArtesanato.Core.Entities.Enums;
using MarketplaceArtesanato.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.API.Controller
{

    [ApiController]
    [Route("api/sellers/analytics-advanced")]
    [Authorize(Roles = "Seller")]
    public class SellerAnalyticsAdvancedController : ControllerBase
    {
        private readonly ISellerAnalyticsAdvancedService _analyticsService;
        private readonly ILogger<SellerAnalyticsAdvancedController> _logger;
        private readonly IAuthorizationService _authorizationService;

        public SellerAnalyticsAdvancedController(
            ISellerAnalyticsAdvancedService analyticsService,
            ILogger<SellerAnalyticsAdvancedController> logger,
            IAuthorizationService authorizationService)
        {
            _analyticsService = analyticsService;
            _logger = logger;
            _authorizationService = authorizationService;
        }


        [HttpGet("dashboard")]
        [Authorize(Policy = "SellerProPremium")]
        public async Task<IActionResult> GetAdvancedDashboard([FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365)
                    return BadRequest(new { message = "Dias deve estar entre 1 e 365" });

                var sellerId = GetSellerIdFromClaims();
                _logger.LogInformation("Vendedor {SellerId} acessando dashboard avançado por {Days} dias", sellerId, days);

                var dashboard = await _analyticsService.GetAdvancedDashboardAsync(sellerId, days);
                return Ok(dashboard);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Vendedor não encontrado: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Acesso não autorizado: {Message}", ex.Message);
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter dashboard avançado");
                return StatusCode(500, new { message = "Erro ao processar solicitação" });
            }
        }

        /// <summary>
        /// Obtém métricas de conversão (clique para compra)
        /// </summary>
        [HttpGet("conversion-metrics")]
        [Authorize(Policy = "SellerProPremium")]
        public async Task<IActionResult> GetConversionMetrics([FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365)
                    return BadRequest(new { message = "Dias deve estar entre 1 e 365" });

                var sellerId = GetSellerIdFromClaims();
                _logger.LogInformation("Obtendo métricas de conversão para vendedor {SellerId}", sellerId);

                var dashboard = await _analyticsService.GetAdvancedDashboardAsync(sellerId, days);
                return Ok(dashboard.ConversionMetrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter métricas de conversão");
                return StatusCode(500, new { message = "Erro ao processar solicitação" });
            }
        }

        [HttpGet("roi-metrics")]
        [Authorize(Policy = "SellerProPremium")]
        public async Task<IActionResult> GetROIMetrics([FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365)
                    return BadRequest(new { message = "Dias deve estar entre 1 e 365" });

                var sellerId = GetSellerIdFromClaims();
                _logger.LogInformation("Obtendo métricas de ROI para vendedor {SellerId}", sellerId);

                var dashboard = await _analyticsService.GetAdvancedDashboardAsync(sellerId, days);
                return Ok(dashboard.ROIMetrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter métricas de ROI");
                return StatusCode(500, new { message = "Erro ao processar solicitação" });
            }
        }

        [HttpGet("customer-analysis")]
        [Authorize(Policy = "SellerProPremium")]
        public async Task<IActionResult> GetCustomerAnalysis([FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365)
                    return BadRequest(new { message = "Dias deve estar entre 1 e 365" });

                var sellerId = GetSellerIdFromClaims();
                _logger.LogInformation("Obtendo análise de clientes para vendedor {SellerId}", sellerId);

                var dashboard = await _analyticsService.GetAdvancedDashboardAsync(sellerId, days);
                return Ok(new
                {
                    cohortAnalysis = dashboard.CustomerAnalysis,
                    lifetimeValue = dashboard.LifetimeValueAnalysis
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter análise de clientes");
                return StatusCode(500, new { message = "Erro ao processar solicitação" });
            }
        }

        [HttpGet("period-comparison")]
        [Authorize(Policy = "SellerProPremium")]
        public async Task<IActionResult> GetPeriodComparison([FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365)
                    return BadRequest(new { message = "Dias deve estar entre 1 e 365" });

                var sellerId = GetSellerIdFromClaims();
                _logger.LogInformation("Obtendo comparativo de períodos para vendedor {SellerId}", sellerId);

                var comparison = await _analyticsService.GetPeriodComparisonAsync(sellerId, days);
                return Ok(comparison);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter comparativo de períodos");
                return StatusCode(500, new { message = "Erro ao processar solicitação" });
            }
        }

        [HttpGet("sales-forecast")]
        [Authorize(Policy = "SellerProPremium")]
        public async Task<IActionResult> GetSalesForecast(
            [FromQuery] int historicalDays = 30,
            [FromQuery] int forecastDays = 30)
        {
            try
            {
                if (historicalDays < 1 || historicalDays > 365 || forecastDays < 1 || forecastDays > 365)
                    return BadRequest(new { message = "Dias deve estar entre 1 e 365" });

                var sellerId = GetSellerIdFromClaims();
                _logger.LogInformation("Gerando previsão de vendas para vendedor {SellerId}", sellerId);

                var forecast = await _analyticsService.GenerateSalesForecastAsync(sellerId, historicalDays, forecastDays);
                return Ok(forecast);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao gerar previsão de vendas");
                return StatusCode(500, new { message = "Erro ao processar solicitação" });
            }
        }


        [HttpGet("products-performance")]
        [Authorize(Policy = "SellerProPremium")]
        public async Task<IActionResult> GetProductsPerformance([FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365)
                    return BadRequest(new { message = "Dias deve estar entre 1 e 365" });

                var sellerId = GetSellerIdFromClaims();
                _logger.LogInformation("Obtendo performance de produtos para vendedor {SellerId}", sellerId);

                var dashboard = await _analyticsService.GetAdvancedDashboardAsync(sellerId, days);
                return Ok(new
                {
                    topProducts = dashboard.TopProducts,
                    categoryPerformance = dashboard.CategoryPerformance
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter performance de produtos");
                return StatusCode(500, new { message = "Erro ao processar solicitação" });
            }
        }

        [HttpPost("export")]
        [Authorize(Policy = "SellerProPremium")]
        public async Task<IActionResult> ExportAnalytics(
            [FromBody] ExportRequest request)
        {
            try
            {
                if (request.PeriodStart >= request.PeriodEnd)
                    return BadRequest(new { message = "Data inicial deve ser anterior à data final" });

                if ((request.PeriodEnd - request.PeriodStart).TotalDays > 365)
                    return BadRequest(new { message = "Período máximo é de 365 dias" });

                var format = request.Format?.ToUpper() ?? "PDF";
                if (!new[] { "PDF", "CSV", "EXCEL" }.Contains(format))
                    return BadRequest(new { message = "Formato deve ser PDF, CSV ou EXCEL" });

                var sellerId = GetSellerIdFromClaims();
                _logger.LogInformation("Exportando analytics para vendedor {SellerId} em formato {Format}", sellerId, format);

                var export = await _analyticsService.GenerateExportAsync(
                    sellerId,
                    request.PeriodStart,
                    request.PeriodEnd,
                    format);

                return Ok(new
                {
                    message = $"Relatório gerado com sucesso em formato {format}",
                    export = export,
                    downloadUrl = $"/api/sellers/analytics-advanced/export-download/{export.SellerId}"
                });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Vendedor não encontrado: {Message}", ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao exportar analytics");
                return StatusCode(500, new { message = "Erro ao processar solicitação" });
            }
        }


        [HttpGet("check-access")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CheckAccess()
        {
            try
            {
                var result = await _authorizationService.AuthorizeAsync(
                    User,
                    null,
                    new SellerPlanRequirement(SellerPlan.Pro));

                if (result.Succeeded)
                {
                    var plan = User.FindFirst("seller_plan")?.Value ?? "Unknown";
                    return Ok(new
                    {
                        hasAccess = true,
                        plan = plan,
                        message = "Você tem acesso a analytics avançado"
                    });
                }

                return Ok(new
                {
                    hasAccess = false,
                    message = "Você não tem plano Pro ou Premium. Faça upgrade para acessar analytics avançado.",
                    upgradeUrl = "/sellers/subscription/upgrade"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar acesso");
                return StatusCode(500, new { message = "Erro ao processar solicitação" });
            }
        }

        // ============================================================================
        // MÉTODO PRIVADO
        // ============================================================================

        private Guid GetSellerIdFromClaims()
        {
            var sellerIdClaim = User.FindFirst("seller_id")?.Value;
            if (string.IsNullOrEmpty(sellerIdClaim))
            {
                throw new UnauthorizedAccessException("Vendedor não identificado");
            }

            if (!Guid.TryParse(sellerIdClaim, out var sellerId))
            {
                throw new InvalidOperationException("ID de vendedor inválido");
            }

            return sellerId;
        }
    }

    /// <summary>
    /// Modelo de requisição para exportação
    /// </summary>
    public class ExportRequest
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public string? Format { get; set; } = "PDF";
    }
}

