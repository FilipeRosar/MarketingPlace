namespace MarketplaceArtesanato.Core.Entities.Models.Responses
{
    public class InstallmentSettingsDto
    {
        public decimal InterestRateMonthly { get; set; }
        public int MaxInstallments { get; set; }
        public decimal MinInstallmentAmount { get; set; }
        public string Rounding { get; set; } = "round";
        public bool GatewayFeeEmbedded { get; set; } = true;
        public string AnticipationPolicy { get; set; } = string.Empty;
    }
}
