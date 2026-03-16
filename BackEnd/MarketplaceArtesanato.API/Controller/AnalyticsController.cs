using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Core.Entities.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace MarketplaceArtesanato.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Recebe lote de eventos de analytics do frontend
        /// </summary>
        /// <remarks>
        /// Endpoint público para receber eventos sem autenticação.
        /// Eventos incluem visualizações, cliques, erros e interações do usuário.
        /// </remarks>
        /// <param name="batch">Lote de eventos para registrar</param>
        [HttpPost("events")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LogAnalyticsEvents([FromBody] AnalyticsEventBatchDto batch)
        {
            // Validação básica
            if (batch?.Events == null || batch.Events.Count == 0)
            {
                return BadRequest(new { message = "No events provided" });
            }

            // Limite de segurança: máximo 100 eventos por requisição
            if (batch.Events.Count > 100)
            {
                return BadRequest(new 
                { 
                    message = "Too many events in single request", 
                    limit = 100, 
                    received = batch.Events.Count 
                });
            }

            try
            {
                // Processar cada evento
                foreach (var evt in batch.Events)
                {
                    // Adicionar contexto de requisição
                    evt.UserAgent = Request.Headers["User-Agent"].ToString();
                    evt.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    evt.Timestamp = DateTime.UtcNow;

                    // Log básico dos eventos (idealmente usar logger estruturado)
                    System.Diagnostics.Debug.WriteLine(
                        $"[ANALYTICS] {evt.EventName} | {evt.EventCategory} | {evt.EventLabel} | {evt.Timestamp:yyyy-MM-dd HH:mm:ss}");
                }

                // Responder com sucesso
                return Ok(new 
                { 
                    message = "Events received successfully",
                    count = batch.Events.Count,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ANALYTICS ERROR] Failed to log events: {ex.Message}");
                
                // Retornar erro sem expor detalhes internos
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { message = "Error processing analytics events" }
                );
            }
        }

        /// <summary>
        /// Obter analytics geral da plataforma
        /// </summary>
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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
