using MarketplaceArtesanato.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class SubscribeRequestDto
    {
        public Guid SellerId { get; set; }
        public SellerPlan Plan { get; set; }
    }
}
