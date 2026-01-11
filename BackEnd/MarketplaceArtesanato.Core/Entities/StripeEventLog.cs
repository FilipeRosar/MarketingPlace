using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities
{
    public class StripeEventLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string EventId { get; set; } = null!;   
        public string EventType { get; set; } = null!;

        public DateTime ProcessedAt { get; set; }
    }
}
