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
        public ShippingAddressDTO? ShippingAddress { get; set; }
        public decimal ShippingFee { get; set; } = 0m;
        public string? ShippingName { get; set; }
        public decimal ShippingCost { get; set; }
        [Required]
        public string SuccessUrl { get; set; } = string.Empty;
        public string? Carrier { get; set; }
        [Required]
        public string CancelUrl { get; set; } = string.Empty;
        public string? CouponCode { get; set; }
    }

    public class CheckoutItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
