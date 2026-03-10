using MarketplaceArtesanato.Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface ICouponAutomationService
    {
        /// <summary>
        /// Desativa cupons que expiram automaticamente
        /// </summary>
        Task DeactivateExpiredCouponsAsync();

        /// <summary>
        /// Aplica limites de uso automáticos em cupons baseado em regras
        /// </summary>
        Task ApplyAutomaticLimitsAsync();

        /// <summary>
        /// Ativa/desativa cupons sazonais com base em datas configuradas
        /// </summary>
        Task ApplySeasonalCouponsAsync();

        /// <summary>
        /// Executa todas as automações (chamado pelo CronJob)
        /// </summary>
        Task ExecuteAllAutomationsAsync();

        /// <summary>
        /// Obtém status das automações recentes
        /// </summary>
        Task<List<CouponAutomationLogDto>> GetAutomationLogsAsync(int days = 7);
    }

    public class CouponAutomationLogDto
    {
        public Guid Id { get; set; }
        public string AutomationType { get; set; } // "Expiration", "Limit", "Seasonal"
        public DateTime ExecutedAt { get; set; }
        public int AffectedCoupons { get; set; }
        public string Status { get; set; } // "Success", "Failed", "PartialFail"
        public string Message { get; set; }
        public string Details { get; set; }
    }
}
