using MarketplaceArtesanato.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.Models.Responses
{
    public class SellerSaleResponseDto
    {
        public Guid OrderId { get; set; }
        public string DisplayOrderId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = "Cliente Trama";
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? TrackingCode { get; set; }
        public string? Carrier { get; set; }
        public List<SellerSaleItemDto> Items { get; set; } = new();
    }

    public class SellerSaleItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }
}
