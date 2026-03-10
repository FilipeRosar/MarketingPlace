using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class ConfirmEmailDto
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
