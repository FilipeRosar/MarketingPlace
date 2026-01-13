using MarketplaceArtesanato.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class OrderDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid BuyerId { get; set; }

        public string BuyerName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ShippedAt { get; set; }

        [Required]
        public decimal Total { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal SubTotal { get; set; }

        [Required]
        public OrderStatus Status { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? StripeSessionId { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public string? TrackingCode { get; set; }
        public List<string>? TrackingCodes { get; set; }
        public ShippingAddressDTO? ShippingAddress { get; set; }
        public decimal TotalAmount { get; set; }
        public string StatusText { get; set; }
        public string Carrier { get; set; }
        public string? SellerName { get; set; }
        [Required]
        public List<OrderItemDto> Items { get; set; } = new();
        public Dictionary<Guid, decimal>? SellerCommissions { get; set; }

        public decimal? MyCommission { get; set; }
    }
}
