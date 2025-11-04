using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.Enums
{
    public enum OrderStatus
    {
        Pending,
        Paid,
        Processing,
        Sent,
        Delivered,
        Canceled
    }
}
