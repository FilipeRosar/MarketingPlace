using MarketplaceArtesanato.Core.Entities.Models.Responses;
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
        public string? Bio { get; set; } 
        public string? BannerImageUrl { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? Instagram { get; set; }
        public string? Facebook { get; set; }
        public string? Tiktok { get; set; }
        public string? Youtube { get; set; }
        public List<MomentResponseDto> Moments { get; set; } = new List<MomentResponseDto>();
        public AddressResponseDto Address { get; set; } = new();

    }
}
