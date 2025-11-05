using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Events
{
    public class CheckoutFailedEvent
    {
        public Guid CustomerId { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}
