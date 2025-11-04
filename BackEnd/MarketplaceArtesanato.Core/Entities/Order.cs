using MarketplaceArtesanato.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities
{
    [Table("Orders")]
    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid BuyerId { get; set; }
        public Customer Buyer { get; set; } = null!;

        public Guid? SellerId { get; set; }
        public Seller? Seller { get; set; }

        public List<OrderItem> Items { get; set; } = new();
        public decimal Total { get; set; }
        public string StripeSessionId { get; set; } = string.Empty;
        public string? StripePaymentIntentId { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

}
