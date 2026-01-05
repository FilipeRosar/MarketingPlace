using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.Models.Responses
{
    public class MomentResponseDto
    {
        public Guid Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public string VideoUrl { get; set; } = string.Empty;
        public string? ThumbUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
