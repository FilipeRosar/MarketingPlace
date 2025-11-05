using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Events
{
    public class OrderPaidEvent
    {
        public Guid OrderId { get; set; }
        public decimal Total { get; set; }
        public Dictionary<Guid, decimal> SellerCommissions { get; set; } = new();
    }
}
