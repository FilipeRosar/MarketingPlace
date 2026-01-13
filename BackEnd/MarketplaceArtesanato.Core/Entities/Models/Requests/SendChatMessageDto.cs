using System;

namespace MarketplaceArtesanato.Core.Entities.Models.Requests
{
    public class SendChatMessageDto
    {
        public Guid RecipientUserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
