using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Events
{
    public class CheckoutItemEvent
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
