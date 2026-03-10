using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    /// <summary>
    /// Serviço de Analytics Geral da Plataforma
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// Obter analytics geral da plataforma
        /// </summary>
        Task<PlatformAnalyticsDto> GetPlatformAnalyticsAsync();

        /// <summary>
        /// Obter top produtos por vendas
        /// </summary>
        Task<List<TopProductDto>> GetTopProductsAsync(int limit = 10);

        /// <summary>
        /// Obter estatísticas de usuários
        /// </summary>
        Task<UserAnalyticsDto> GetUserAnalyticsAsync();

        /// <summary>
        /// Obter vendas por período (últimos 12 meses)
        /// </summary>
        Task<List<SalesPeriodDto>> GetSalesByPeriodAsync();

        /// <summary>
        /// Obter distribuição de produtos por categoria
        /// </summary>
        Task<List<CategoryDistributionDto>> GetCategoryDistributionAsync();

        /// <summary>
        /// Obter status de saúde da plataforma
        /// </summary>
        Task<PlatformHealthDto> GetPlatformHealthAsync();

        /// <summary>
        /// Obter dados de vendas por seller
        /// </summary>
        Task<List<CommissionReportItemResponse>> GetSellerPerformanceAsync();

        /// <summary>
        /// Obter dados de conversão por funil
        /// </summary>
        Task<Dictionary<string, int>> GetConversionFunnelAsync();
    }
}
