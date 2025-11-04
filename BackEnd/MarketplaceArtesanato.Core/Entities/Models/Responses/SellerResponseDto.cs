using System.Text.Json.Serialization;

namespace MarketplaceArtesanato.API.Models.Responses
{
    public class SellerResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CPF { get; set; }
        public string? CNPJ { get; set; }
        public AddressResponseDto Address { get; set; } = null!;
    }
}
