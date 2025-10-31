using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class CreateRatingDto
    {
        [Required, Range(1, 5)]
        public int Stars { get; set; }

        [StringLength(500)]
        public string Review { get; set; } = string.Empty;
    }
}
