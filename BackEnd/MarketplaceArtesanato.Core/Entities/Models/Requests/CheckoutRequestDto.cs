using MarketplaceArtesanato.Core.Entities.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.Models.Requests
{
    public class CheckoutRequestDto
    {
        public List<CartItemDto> Items { get; set; } = new();
    }
}
