using MarketplaceArtesanato.Core.Interfaces;
using MarketplaceArtesanato.Data.Data;
using MarketplaceArtesanato.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceArtesanato.Services.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly ArtesianDbContext _context;

        public SettingsService(ArtesianDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetCommissionRateAsync()
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == "PlatformCommissionRate");

            if (setting != null && decimal.TryParse(setting.Value, out var rate))
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

            if (setting != null && decimal.TryParse(setting.Value, out var fee))
            {
                return fee;
            }

            return 2.99m; // Padrão
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
    }
}
