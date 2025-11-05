using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Events
{
    public class OrderCreatedEvent
    {
        public Guid OrderId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Total { get; set; }
    }
}
