using System.Text.Json.Serialization;

namespace MarketplaceArtesanato.API.Models.Responses
{
    public class SellerResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Phone { get; set; }

        [JsonPropertyName("address")]
        public AddressResponseDto Address { get; set; } = null!;

        public DateTime CreatedAt { get; set; }
    }
}
