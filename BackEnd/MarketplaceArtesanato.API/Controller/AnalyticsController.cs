using MarketplaceArtesanato.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Obter analytics geral da plataforma
        /// </summary>
        [HttpGet("platform")]
        public async Task<IActionResult> GetPlatformAnalytics()
        {
            try
            {
                var analytics = await _analyticsService.GetPlatformAnalyticsAsync();
                return Ok(analytics);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter top 10 produtos por vendas
        /// </summary>
        [HttpGet("top-products")]
        public async Task<IActionResult> GetTopProducts([FromQuery] int limit = 10)
        {
            try
            {
                var products = await _analyticsService.GetTopProductsAsync(limit);
                return Ok(products);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter estatísticas de usuários
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUserAnalytics()
        {
            try
            {
                var analytics = await _analyticsService.GetUserAnalyticsAsync();
                return Ok(analytics);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter vendas por período (últimos 12 meses)
        /// </summary>
        [HttpGet("sales-period")]
        public async Task<IActionResult> GetSalesByPeriod()
        {
            try
            {
                var sales = await _analyticsService.GetSalesByPeriodAsync();
                return Ok(sales);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter distribuição de produtos por categoria
        /// </summary>
        [HttpGet("category-distribution")]
        public async Task<IActionResult> GetCategoryDistribution()
        {
            try
            {
                var distribution = await _analyticsService.GetCategoryDistributionAsync();
                return Ok(distribution);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter status de saúde da plataforma
        /// </summary>
        [HttpGet("health")]
        public async Task<IActionResult> GetPlatformHealth()
        {
            try
            {
                var health = await _analyticsService.GetPlatformHealthAsync();
                return Ok(health);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter performance de vendedores
        /// </summary>
        [HttpGet("sellers")]
        public async Task<IActionResult> GetSellerPerformance()
        {
            try
            {
                var sellers = await _analyticsService.GetSellerPerformanceAsync();
                return Ok(sellers);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obter funil de conversão
        /// </summary>
        [HttpGet("conversion-funnel")]
        public async Task<IActionResult> GetConversionFunnel()
        {
            try
            {
                var funnel = await _analyticsService.GetConversionFunnelAsync();
                return Ok(funnel);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
