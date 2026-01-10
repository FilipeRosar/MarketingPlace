using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceArtesanato.Services.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ArtesianDbContext _context;
        private const decimal DefaultInterestRateMonthly = 2.99m;
        private const int DefaultMaxInstallments = 12;
        private const decimal DefaultMinInstallmentAmount = 20m;
        private const string DefaultRounding = "round";
        private const bool DefaultGatewayFeeEmbedded = true;
        private const string DefaultAnticipationPolicy = "";

        public SettingsService(ArtesianDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetCommissionRateAsync()
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "PlatformCommissionRate");

            if (setting != null && decimal.TryParse(setting.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate))
            {
                return rate;
            }

            // Valor padrão se não existir no banco
            return 0.15m;
        }

        public async Task<decimal> GetServiceFeeAsync()
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "ServiceFee");

            if (setting != null && decimal.TryParse(setting.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var fee))
            {
                return fee;
            }

            return 2.99m; // Padrão
        }

        public async Task<InstallmentSettingsDto> GetInstallmentSettingsAsync()
        {
            var settings = await _context.SystemSettings
                .Where(s =>
                    s.Key == "InstallmentInterestRateMonthly" ||
                    s.Key == "InstallmentMax" ||
                    s.Key == "InstallmentMinAmount" ||
                    s.Key == "InstallmentRounding" ||
                    s.Key == "InstallmentGatewayFeeEmbedded" ||
                    s.Key == "InstallmentAnticipationPolicy")
                .ToDictionaryAsync(s => s.Key, s => s.Value);

            return new InstallmentSettingsDto
            {
                InterestRateMonthly = TryParseDecimal(settings, "InstallmentInterestRateMonthly", DefaultInterestRateMonthly),
                MaxInstallments = TryParseInt(settings, "InstallmentMax", DefaultMaxInstallments),
                MinInstallmentAmount = TryParseDecimal(settings, "InstallmentMinAmount", DefaultMinInstallmentAmount),
                Rounding = TryParseString(settings, "InstallmentRounding", DefaultRounding),
                GatewayFeeEmbedded = TryParseBool(settings, "InstallmentGatewayFeeEmbedded", DefaultGatewayFeeEmbedded),
                AnticipationPolicy = TryParseString(settings, "InstallmentAnticipationPolicy", DefaultAnticipationPolicy)
            };
        }

        public async Task UpdateInstallmentSettingsAsync(InstallmentSettingsDto dto)
        {
            await UpsertSettingAsync("InstallmentInterestRateMonthly", dto.InterestRateMonthly.ToString());
            await UpsertSettingAsync("InstallmentMax", dto.MaxInstallments.ToString());
            await UpsertSettingAsync("InstallmentMinAmount", dto.MinInstallmentAmount.ToString());
            await UpsertSettingAsync("InstallmentRounding", dto.Rounding ?? DefaultRounding);
            await UpsertSettingAsync("InstallmentGatewayFeeEmbedded", dto.GatewayFeeEmbedded.ToString());
            await UpsertSettingAsync("InstallmentAnticipationPolicy", dto.AnticipationPolicy ?? string.Empty);
        }

        public async Task UpdateSettingAsync(string key, string value)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                setting = new SystemSetting { Id = Guid.NewGuid(), Key = key, Value = value };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<SystemSetting>> GetAllAsync()
        {
            return await _context.SystemSettings.ToListAsync();
        }

        private async Task UpsertSettingAsync(string key, string value)
        {
            var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);

            if (setting == null)
            {
                setting = new SystemSetting { Id = Guid.NewGuid(), Key = key, Value = value };
                _context.SystemSettings.Add(setting);
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        private static decimal TryParseDecimal(
            IReadOnlyDictionary<string, string> settings,
            string key,
            decimal fallback)
        {
            return settings.TryGetValue(key, out var value) && decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        private static int TryParseInt(
            IReadOnlyDictionary<string, string> settings,
            string key,
            int fallback)
        {
            return settings.TryGetValue(key, out var value) && int.TryParse(value, out var parsed)
                ? parsed
                : fallback;
        }

        private static bool TryParseBool(
            IReadOnlyDictionary<string, string> settings,
            string key,
            bool fallback)
        {
            return settings.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed)
                ? parsed
                : fallback;
        }

        private static string TryParseString(
            IReadOnlyDictionary<string, string> settings,
            string key,
            string fallback)
        {
            return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
                ? value
                : fallback;
        }
    }
}