using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class AddressDto
    {
        [Required] public string Street { get; set; } = string.Empty;
        [Required] public string Number { get; set; } = string.Empty;
        [Required] public string City { get; set; } = string.Empty;
        [Required] public string State { get; set; } = string.Empty;
        [Required] public string ZipCode { get; set; } = string.Empty;
        [Required] public string Country { get; set; } 
    }
}
