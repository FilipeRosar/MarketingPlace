using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class OrderItemDto
    {
        [Required]
        public Guid ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        public string? ProductImage { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        public decimal Subtotal => UnitPrice * Quantity;

        [Required]
        public Guid SellerId { get; set; }

        public string SellerName { get; set; } = string.Empty;
    }
}
