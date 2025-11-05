using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Events
{
    public class CheckoutInitiatedEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid CustomerId { get; set; }
        public List<CheckoutItemEvent> Items { get; set; } = new();
        public DateTime InitiatedAt { get; set; }
    }
}
