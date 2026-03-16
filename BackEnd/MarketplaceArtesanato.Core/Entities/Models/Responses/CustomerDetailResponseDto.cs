using System;
using System.Text.Json.Serialization;

namespace MarketplaceArtesanato.Core.Entities.Models.Responses
{
    public class CustomerDetailResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CPF { get; set; }
        public string? ProfileImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? BirthDate { get; set; }
        public bool NewsletterSubscribed { get; set; }
        public int LoyaltyPoints { get; set; }
        public string? LastOrderDate { get; set; }
        public decimal TotalSpent { get; set; }
        public int OrderCount { get; set; }
        public DateTime? BannedAt { get; set; }
        
        [JsonPropertyName("isBanned")]
        public bool IsBanned => BannedAt.HasValue;
    }
}
