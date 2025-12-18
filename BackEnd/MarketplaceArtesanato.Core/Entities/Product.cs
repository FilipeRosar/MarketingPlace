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
        public decimal? SalePrice { get; set; }
        [Required]
        public int StockQuantity { get; set; } = 0;
        public string Tags { get; set; } = string.Empty;
        public List<ProductImage> Images { get; set; } = new();
        public ProductCategory Category { get; set; } 
        public ProductStatus Status { get; set; }
        public List<Rating> Ratings { get; set; } = new List<Rating>();
        public Guid SellerId { get; set; }
        public Seller Seller { get; set; }
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

}
