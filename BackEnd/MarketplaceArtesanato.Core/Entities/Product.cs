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
    [Table("Products")]
    public class Product : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        public Guid CategoryId { get; set; }
        public decimal? SalePrice { get; set; }
        public DateTime? BoostedUntil { get; set; }
        public int MaxInstallments { get; set; } = 12;
        public int MaxNoInterestInstallments { get; set; } = 0;
        [Required]
        public int StockQuantity { get; set; } = 0;
        public string Tags { get; set; } = string.Empty;
        public List<ProductImage> Images { get; set; } = new();
        public ProductCategory Category { get; set; }
        public decimal? OriginalPrice { get; set; }
        public bool HasDiscount { get; set; }

        public ProductStatus Status { get; set; }
        public List<Rating> Ratings { get; set; } = new List<Rating>();
        public Guid SellerId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Weight { get; set; } 
        public int Width { get; set; }  
        public int Height { get; set; } 
        public int Length { get; set; }
        public Seller Seller { get; set; }
        public bool StoryEnabled { get; set; } = false;
        [StringLength(200)]
        public string? StoryMaker { get; set; }
        [StringLength(200)]
        public string? StoryExperience { get; set; }
        [StringLength(500)]
        public string? StoryInspiration { get; set; }
        public string? StoryMarkdown { get; set; }
        public List<ProductStoryMedia> StoryMedia { get; set; } = new();

        public bool IsOnSale => SalePrice.HasValue && SalePrice.Value > 0 && SalePrice.Value < Price;

    }
    public class ProductImage
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public Guid ProductId { get; set; }
        public bool IsMain { get; set; } = false;
        public Product Product { get; set; } = null!;
    }

    public class ProductStoryMedia
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}

