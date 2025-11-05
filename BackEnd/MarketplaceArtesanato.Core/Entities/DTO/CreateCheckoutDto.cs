using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class CreateCheckoutDto
    {
        public Guid? AddressId { get; set; }
        public string? Notes { get; set; }
    }
}
