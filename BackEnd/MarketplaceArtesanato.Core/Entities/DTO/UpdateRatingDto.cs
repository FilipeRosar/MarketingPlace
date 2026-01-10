using System;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class UpdateRatingDto
    {
        [Required]
        [Range(1, 5)]
        public int Stars { get; set; }

        [MaxLength(500)]
        public string? Review { get; set; }
    }
}
