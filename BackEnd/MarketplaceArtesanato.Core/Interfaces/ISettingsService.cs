using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Models.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Interfaces
{
    public interface ISettingsService
    {
        Task<decimal> GetCommissionRateAsync();
        Task<decimal> GetServiceFeeAsync();
        Task<InstallmentSettingsDto> GetInstallmentSettingsAsync();
        Task UpdateInstallmentSettingsAsync(InstallmentSettingsDto dto);
        Task UpdateSettingAsync(string key, string value);
        Task<List<SystemSetting>> GetAllAsync();
    }
}
