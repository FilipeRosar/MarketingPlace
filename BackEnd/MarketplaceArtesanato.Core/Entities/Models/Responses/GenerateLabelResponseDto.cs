namespace MarketplaceArtesanato.Core.Entities.Models.Responses
{
    public class GenerateLabelResponseDto
    {
        public string LabelUrl { get; set; } = string.Empty;
        public string? Warning { get; set; }
    }
}
