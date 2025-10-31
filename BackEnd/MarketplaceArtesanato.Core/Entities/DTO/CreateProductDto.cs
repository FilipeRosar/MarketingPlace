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
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required, Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Required, Range(0, 100000)]
        public int StockQuantity { get; set; }

        [Required]
        public List<IFormFile> Images { get; set; } = new();

        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductCategory Category { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ProductStatus Status { get; set; } = ProductStatus.Active;

    }
    public class UpdateProductDto : CreateProductDto
    {
        public Guid Id { get; set; }
    }
}
