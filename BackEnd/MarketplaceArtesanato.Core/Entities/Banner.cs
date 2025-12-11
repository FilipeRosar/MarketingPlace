using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities
{
    [Table("Banners")]
    public class Banner : BaseEntity
    {
        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Subtitle { get; set; }

        [Required]
        public string ImageUrl { get; set; } = string.Empty;

        public string? LinkUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } 
    }
}
