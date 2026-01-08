using MarketplaceArtesanato.Core.Entities.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Models.Requests
{
    public class CheckoutRequestDto
    {
        [Required]
        public List<CheckoutItemDto> Items { get; set; } = new();

        public decimal ShippingFee { get; set; } = 0m;
        public string? ShippingName { get; set; }

        [Required]
        public string SuccessUrl { get; set; } = string.Empty;

        [Required]
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class CheckoutItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
