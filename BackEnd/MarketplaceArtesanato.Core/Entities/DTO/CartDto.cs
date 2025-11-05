using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class CartDto
    {
        public Guid CustomerId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
        public int TotalItems => Items.Sum(i => i.Quantity);
        public decimal TotalPrice => Items.Sum(i => i.Quantity * i.Quantity);
    }

}
