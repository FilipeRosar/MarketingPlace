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
    }

    public class UpdateBannerDto
    {
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? LinkUrl { get; set; }
        public bool? IsActive { get; set; }
        public int? DisplayOrder { get; set; }
        public IFormFile? Image { get; set; } 
    }
}
