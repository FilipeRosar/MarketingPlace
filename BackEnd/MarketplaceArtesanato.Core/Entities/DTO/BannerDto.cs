using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class CreateBannerDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? LinkUrl { get; set; }
        public int DisplayOrder { get; set; }

        [Required]
        public IFormFile Image { get; set; } = null!;

        // Styling fields
        public string? BackgroundColor { get; set; } = "#ffffff";
        public string? FontFamily { get; set; } = "Arial, sans-serif";
        public string? FontColor { get; set; } = "#1f2937";
        public int? FontSizeTitle { get; set; } = 48;
        public int? FontSizeSubtitle { get; set; } = 18;

        // Image dimensions
        public int? ImageWidth { get; set; } = 1200;
        public int? ImageHeight { get; set; } = 400;
        public string? ImageObjectFit { get; set; } = "cover";
    }

    public class UpdateBannerDto
    {
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? LinkUrl { get; set; }
        public bool? IsActive { get; set; }
        public int? DisplayOrder { get; set; }
        public IFormFile? Image { get; set; }

        // Styling fields
        public string? BackgroundColor { get; set; }
        public string? FontFamily { get; set; }
        public string? FontColor { get; set; }
        public int? FontSizeTitle { get; set; }
        public int? FontSizeSubtitle { get; set; }

        // Image dimensions
        public int? ImageWidth { get; set; }
        public int? ImageHeight { get; set; }
        public string? ImageObjectFit { get; set; }
    }
}
