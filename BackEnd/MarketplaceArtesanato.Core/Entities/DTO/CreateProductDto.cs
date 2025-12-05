using MarketplaceArtesanato.Core.Entities.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class CreateProductDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [Required]
        public int StockQuantity { get; set; }
        [Required]
        public List<string> Tags { get; set; } = new();

        [Required]
        public ProductCategory Category { get; set; }

        [Required]
        public List<IFormFile> Images { get; set; } = new();
    }
    public class UpdateProductDto
    {
        public Guid Id { get; set; }
        [StringLength(100)] public string? Name { get; set; }
        [StringLength(1000)] public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int? StockQuantity { get; set; }
        public ProductCategory? Category { get; set; }
        public ProductStatus? Status { get; set; }
    }
}
