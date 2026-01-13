using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Entities.DTO;
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
        public decimal? SalePrice { get; set; }
        public DateTime? BoostedUntil { get; set; }
        public bool IsBoosted { get; set; }
        public int MaxInstallments { get; set; }
        public int MaxNoInterestInstallments { get; set; }
        public decimal Weight { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Length { get; set; }
        [Required]
        public List<string> Tags { get; set; } = new();

        [Required]
        public int StockQuantity { get; set; } = 0;
        public List<ProductImageDto> Images { get; set; } = new();
        public string ImageUrl => Images.FirstOrDefault()?.Url ?? "";
        public ProductCategory Category { get; set; }
        public ProductStatus Status { get; set; }
        public double AverageRating { get; set; }  
        public int TotalRatings { get; set; }
        public Guid SellerId { get; set; }
        [JsonPropertyName("seller")]
        public SellerResponseDto Seller { get; set; }
        public string SellerName => Seller?.Name ?? "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool StoryEnabled { get; set; }
        public string? StoryMaker { get; set; }
        public string? StoryExperience { get; set; }
        public string? StoryInspiration { get; set; }
        public string? StoryMarkdown { get; set; }
        public List<string> StoryMediaUrls { get; set; } = new();
    }
}
