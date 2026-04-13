using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Services.Services
{
    public class CouponAutomationService : ICouponAutomationService
    {
        private readonly ArtesianDbContext _context;

        private static List<CouponAutomationLog> _automationLogs = new();

        public CouponAutomationService(ArtesianDbContext context)
        {
            _context = context;
        }

        public async Task DeactivateExpiredCouponsAsync()
        {
            var logEntry = new CouponAutomationLog
            {
                Id = Guid.NewGuid(),
                AutomationType = "Expiration",
                ExecutedAt = DateTime.UtcNow,
                Status = "Running"
            };

            try
            {
                var expiredCoupons = await _context.Coupons
                    .Where(c => c.IsActive && c.ValidUntil <= DateTime.UtcNow && !c.IsDeleted)
                    .ToListAsync();

                if (expiredCoupons.Any())
                {
                    foreach (var coupon in expiredCoupons)
                    {
                        coupon.IsActive = false;
                        coupon.UpdatedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                    logEntry.AffectedCoupons = expiredCoupons.Count;
                    logEntry.Status = "Success";
                    logEntry.Message = $"{expiredCoupons.Count} cupons expirados foram desativados.";
                }
                else
                {
                    logEntry.AffectedCoupons = 0;
                    logEntry.Status = "Success";
                    logEntry.Message = "Nenhum cupom expirado encontrado.";
                }
            }
            catch (Exception ex)
            {
                logEntry.Status = "Failed";
                logEntry.Message = "Erro ao desativar cupons expirados.";
                logEntry.Details = ex.Message;
            }

            _automationLogs.Add(logEntry);
        }

        public async Task ApplyAutomaticLimitsAsync()
        {
            var logEntry = new CouponAutomationLog
            {
                Id = Guid.NewGuid(),
                AutomationType = "Limit",
                ExecutedAt = DateTime.UtcNow,
                Status = "Running"
            };

            try
            {
                // Encontrar cupons que atingiram o limite de uso
                var couponsWithLimit = await _context.Coupons
                    .Include(c => c.Usages)
                    .Where(c => c.IsActive && c.UsageLimit > 0 && !c.IsDeleted)
                    .ToListAsync();

                var affectedCount = 0;

                foreach (var coupon in couponsWithLimit)
                {
                    var usageCount = coupon.Usages?.Count ?? 0;

                    // Se o uso atingiu o limite, desativar
                    if (usageCount >= coupon.UsageLimit)
                    {
                        coupon.IsActive = false;
                        coupon.UpdatedAt = DateTime.UtcNow;
                        affectedCount++;
                    }
                }

                if (affectedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    logEntry.AffectedCoupons = affectedCount;
                    logEntry.Status = "Success";
                    logEntry.Message = $"{affectedCount} cupons atingiram o limite de uso e foram desativados.";
                }
                else
                {
                    logEntry.AffectedCoupons = 0;
                    logEntry.Status = "Success";
                    logEntry.Message = "Nenhum cupom atingiu o limite de uso.";
                }
            }
            catch (Exception ex)
            {
                logEntry.Status = "Failed";
                logEntry.Message = "Erro ao aplicar limites automáticos.";
                logEntry.Details = ex.Message;
            }

            _automationLogs.Add(logEntry);
        }

        public async Task ApplySeasonalCouponsAsync()
        {
            var logEntry = new CouponAutomationLog
            {
                Id = Guid.NewGuid(),
                AutomationType = "Seasonal",
                ExecutedAt = DateTime.UtcNow,
                Status = "Running"
            };

            try
            {
                var now = DateTime.UtcNow;
                var affectedCount = 0;

                var couponsToActivate = await _context.Coupons
                    .Where(c => !c.IsActive && 
                               c.ValidFrom <= now && 
                               c.ValidUntil >= now && 
                               c.Type == CouponType.Intelligent &&
                               !c.IsDeleted)
                    .ToListAsync();

                foreach (var coupon in couponsToActivate)
                {
                    coupon.IsActive = true;
                    coupon.UpdatedAt = DateTime.UtcNow;
                    affectedCount++;
                }

                // Desativar cupons sazonais cuja data de término chegou
                var couponsToDeactivate = await _context.Coupons
                    .Where(c => c.IsActive && 
                               c.ValidUntil < now && 
                               c.Type == CouponType.Intelligent &&
                               !c.IsDeleted)
                    .ToListAsync();

                foreach (var coupon in couponsToDeactivate)
                {
                    coupon.IsActive = false;
                    coupon.UpdatedAt = DateTime.UtcNow;
                    affectedCount++;
                }

                if (affectedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    logEntry.AffectedCoupons = affectedCount;
                    logEntry.Status = "Success";
                    logEntry.Message = $"{affectedCount} cupons sazonais foram ajustados automaticamente.";
                }
                else
                {
                    logEntry.AffectedCoupons = 0;
                    logEntry.Status = "Success";
                    logEntry.Message = "Nenhum cupom sazonal para ajustar.";
                }
            }
            catch (Exception ex)
            {
                logEntry.Status = "Failed";
                logEntry.Message = "Erro ao aplicar cupons sazonais.";
                logEntry.Details = ex.Message;
            }

            _automationLogs.Add(logEntry);
        }

        public async Task ExecuteAllAutomationsAsync()
        {
            // Executar todas as automações em sequência
            await DeactivateExpiredCouponsAsync();
            await ApplyAutomaticLimitsAsync();
            await ApplySeasonalCouponsAsync();

            // Limpar logs muito antigos (manter apenas últimos 30 dias)
            _automationLogs.RemoveAll(l => l.ExecutedAt < DateTime.UtcNow.AddDays(-30));
        }

        public async Task<List<CouponAutomationLogDto>> GetAutomationLogsAsync(int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);

            return await Task.FromResult(
                _automationLogs
                    .Where(l => l.ExecutedAt >= cutoffDate)
                    .OrderByDescending(l => l.ExecutedAt)
                    .Select(l => new CouponAutomationLogDto
                    {
                        Id = l.Id,
                        AutomationType = l.AutomationType,
                        ExecutedAt = l.ExecutedAt,
                        AffectedCoupons = l.AffectedCoupons,
                        Status = l.Status,
                        Message = l.Message,
                        Details = l.Details
                    })
                    .ToList()
            );
        }
    }

    // Classe interna para rastreamento de logs (em produção, isso seria uma tabela do DB)
    internal class CouponAutomationLog
    {
        public Guid Id { get; set; }
        public string AutomationType { get; set; }
        public DateTime ExecutedAt { get; set; }
        public int AffectedCoupons { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }
}
