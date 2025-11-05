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

        [Required]
        public decimal Total { get; set; }

        [Required]
        public OrderStatus Status { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? StripeSessionId { get; set; }
        public string? StripePaymentIntentId { get; set; }

        [Required]
        public List<OrderItemDto> Items { get; set; } = new();
        public Dictionary<Guid, decimal>? SellerCommissions { get; set; }

        public decimal? MyCommission { get; set; }
    }
}
