using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class AddRatingReplyDto
    {
        [Required]
        [MaxLength(500)]
        public string Reply { get; set; } = string.Empty;
    }
}
