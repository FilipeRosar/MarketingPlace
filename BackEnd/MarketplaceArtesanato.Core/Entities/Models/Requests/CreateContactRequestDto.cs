using System;

namespace MarketplaceArtesanato.Core.Entities.Models.Requests
{
    public class CreateContactRequestDto
    {
        public Guid SellerUserId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
