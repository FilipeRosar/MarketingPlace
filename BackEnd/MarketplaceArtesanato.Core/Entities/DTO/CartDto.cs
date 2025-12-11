using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class CartDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
        public decimal TotalPrice => Items.Sum(i => i.Quantity * i.Price);
        public int TotalProducts => Items.Count;

        public string TotalPriceFormatted => TotalPrice.ToString("C"); 
    }

}
