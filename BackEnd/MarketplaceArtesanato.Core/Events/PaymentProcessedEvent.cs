using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Events
{
    public record PaymentProcessedEvent
    {
        public Guid OrderId { get; init; }
        public Guid CustomerId { get; init; }
        public decimal Total { get; init; }
        public string StripeSessionId { get; init; } = string.Empty;
        public string? PaymentIntentId { get; init; }
        public decimal? Amount { get; set; }
        public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
    }
}
