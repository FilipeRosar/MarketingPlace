using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.Models.Requests
{
    public class CalculateBySellerRequest
    {
        [Required]
        public string ZipCodeTo { get; set; } = string.Empty;

        [Required]
        public List<SellerItemsDto> ItemsBySeller { get; set; } = new();
    }

    public class SellerItemsDto
    {
        [Required]
        public string SellerId { get; set; } = string.Empty;

        [Required]
        public List<ShippingItemDto> Items { get; set; } = new();
    }
}
