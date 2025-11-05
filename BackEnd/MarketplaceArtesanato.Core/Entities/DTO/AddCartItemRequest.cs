using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class AddCartItemRequest
    {
        [Required]
        public Guid ProductId { get; set; }
        [Range(1, 100)] 
        public int Quantity { get; set; } = 1;
    }
}
