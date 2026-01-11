using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class CartItemDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public decimal OriginalPrice { get; set; }
        public bool HasDiscount { get; set; }
        public List<DiscountDetailDto> DiscountDetails { get; set; } = new();
        public decimal Price { get; set; }
        public decimal Weight { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Length { get; set; }
        public int Quantity { get; set; }
        public Guid SellerId { get; set; }
        public decimal Subtotal => Price * Quantity;
        public decimal TotalDiscount => (OriginalPrice - Price) * Quantity;
        public decimal OriginalTotal => OriginalPrice * Quantity;
    }
}
