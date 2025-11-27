using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class FavoriteRequestDto
    {
        [Required]
        public Guid ProductId { get; set; }
    }
}
