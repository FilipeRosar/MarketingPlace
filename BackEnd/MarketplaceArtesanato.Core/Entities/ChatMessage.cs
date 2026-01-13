using System;

namespace MarketplaceArtesanato.Core.Entities
{
    public class ChatMessage : BaseEntity
    {
        public Guid SellerId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid SenderUserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
