using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.Core.Entities
{
    public class Cart : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;
        public List<CartItem> Items { get; set; } = new();
    }
}
