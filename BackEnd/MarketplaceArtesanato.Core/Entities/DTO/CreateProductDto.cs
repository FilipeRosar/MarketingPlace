using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public List<string> Images { get; set; } = new();

        [Required]
        public int Category { get; set; }

        public string Status { get; set; } = "Ativo";

        public List<CreateRatingDto> Ratings { get; set; } = new();

        [Required]
        public Guid SellerId { get; set; }
    }
    public class UpdateProductDto : CreateProductDto
    {
        public Guid Id { get; set; }
    }
}
