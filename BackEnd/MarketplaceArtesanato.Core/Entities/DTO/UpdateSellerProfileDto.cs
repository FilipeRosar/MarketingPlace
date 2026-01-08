using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.Core.Models.Requests
{
    public class UpdateSellerProfileDto
    {
        [Required(ErrorMessage = "O nome da loja é obrigatório.")]
        public string Name { get; set; } = string.Empty;

        public string? Bio { get; set; }
        public string? Instagram { get; set; }
        public string? Facebook { get; set; }
        public string? Tiktok { get; set; }
        public string? Youtube { get; set; }
    }
}
