using System;

namespace MarketplaceArtesanato.Core.Entities.Models.Responses
{
    public class ChatMessageResponseDto
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid SenderUserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class ChatThreadResponseDto
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerImageUrl { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
    }

    public class ChatCustomerThreadResponseDto
    {
        public Guid SellerId { get; set; }
        public Guid SellerUserId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string? SellerImageUrl { get; set; }
        public string LastMessage { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
    }
}
