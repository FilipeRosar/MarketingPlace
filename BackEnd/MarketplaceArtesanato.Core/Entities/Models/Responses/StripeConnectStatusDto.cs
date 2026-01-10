namespace MarketplaceArtesanato.Core.Entities.Models.Responses
{
    public class StripeConnectStatusDto
    {
        public bool IsConnected { get; set; }
        public string? AccountId { get; set; }
        public bool ChargesEnabled { get; set; }
        public bool DetailsSubmitted { get; set; }
    }
}
