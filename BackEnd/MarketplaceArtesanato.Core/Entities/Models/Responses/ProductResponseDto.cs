using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MarketplaceArtesanato.API.Models.Responses
{
    public class ProductResponseDto
    {
        public Guid Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        [Required]
        public int StockQuantity { get; set; } = 0;
        public List<string> Images { get; set; } = new List<string>();
        public string ImageUrl => Images.FirstOrDefault() ?? "";
        public ProductCategory Category { get; set; }
        public ProductStatus Status { get; set; }
        public double AverageRating { get; set; }  
        public int TotalRatings { get; set; }
        public Guid SellerId { get; set; }
        [JsonPropertyName("seller")]
        public SellerResponseDto Seller { get; set; }
        public string SellerName => Seller?.Name ?? "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
