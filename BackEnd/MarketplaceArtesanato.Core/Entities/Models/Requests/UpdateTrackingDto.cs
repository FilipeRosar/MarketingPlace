using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.Models.Requests
{
    public class UpdateTrackingDto
    {
        [Required]
        public string TrackingCode { get; set; } = string.Empty;

        [Required]
        public string Carrier { get; set; } = string.Empty; 
    }
}
