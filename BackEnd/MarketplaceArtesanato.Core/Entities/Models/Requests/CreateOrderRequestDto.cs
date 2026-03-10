using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.Models.Requests
{
    public class CreateOrderRequestDto
    {
        public Guid AddressId { get; set; }

        public string ShippingCarrier { get; set; } = null!;
        public string ShippingService { get; set; } = null!;
        public decimal ShippingCost { get; set; }
        public int ShippingDeadlineDays { get; set; }
    }
}
