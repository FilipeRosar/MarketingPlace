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

        // Styling fields
        public string? BackgroundColor { get; set; } = "#ffffff";
        public string? BackgroundImage { get; set; }
        public string? FontFamily { get; set; } = "Arial, sans-serif";
        public string? FontColor { get; set; } = "#1f2937";
        public int? FontSizeTitle { get; set; } = 48;
        public int? FontSizeSubtitle { get; set; } = 18;

        // Image dimensions
        public int? ImageWidth { get; set; } = 1200;
        public int? ImageHeight { get; set; } = 400;
        public string? ImageObjectFit { get; set; } = "cover"; // cover, contain, fill, etc
    }
}
