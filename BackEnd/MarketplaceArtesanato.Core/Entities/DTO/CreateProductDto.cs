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
        public decimal? SalePrice { get; set; }
        public int MaxInstallments { get; set; } = 12;
        public int MaxNoInterestInstallments { get; set; } = 0;

        [Required]
        public decimal Weight { get; set; }

        [Required]
        public int Width { get; set; }

        [Required]
        public int Height { get; set; }

        [Required]
        public int Length { get; set; }

        [Required]
        public List<string> Tags { get; set; } = new();

        [Required]
        public ProductCategory Category { get; set; }

        [Required]
        public List<IFormFile> Images { get; set; } = new();

        public bool StoryEnabled { get; set; } = false;
        [StringLength(200)]
        public string? StoryMaker { get; set; }
        [StringLength(200)]
        public string? StoryExperience { get; set; }
        [StringLength(500)]
        public string? StoryInspiration { get; set; }
        public string? StoryMarkdown { get; set; }
        public List<IFormFile>? StoryMedia { get; set; }
    }
    public class UpdateProductDto
    {
        public Guid Id { get; set; }
        [StringLength(100)] public string? Name { get; set; }
        [StringLength(1000)] public string? Description { get; set; }
        public decimal? Price { get; set; }
        public decimal? SalePrice { get; set; }
        public bool? BoostProduct { get; set; }
        public int? MaxInstallments { get; set; }
        public int? MaxNoInterestInstallments { get; set; }
        public int? StockQuantity { get; set; }
        public ProductCategory? Category { get; set; }
        public ProductStatus? Status { get; set; }
        public bool? StoryEnabled { get; set; }
        [StringLength(200)] public string? StoryMaker { get; set; }
        [StringLength(200)] public string? StoryExperience { get; set; }
        [StringLength(500)] public string? StoryInspiration { get; set; }
        public string? StoryMarkdown { get; set; }
    }
}
