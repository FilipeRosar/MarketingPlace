using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.Models.Requests
{
    public class CalculateShippingRequest
    {
        public string ZipCodeFrom { get; set; } = string.Empty; 

        [Required]
        public string ZipCodeTo { get; set; } = string.Empty;
        
        public Guid? SellerId { get; set; }

        public List<ShippingItemDto> Items { get; set; } = new();
    }

    public class ShippingItemDto
    {
        public double Weight { get; set; }
        public double Height { get; set; } 
        public double Width { get; set; }  
        public double Length { get; set; }
        public int Quantity { get; set; }
    }

    public class ShippingOptionDto
    {
        public string Name { get; set; } = string.Empty; 
        public decimal Price { get; set; }
        public int DeliveryTime { get; set; } 
        public string? CompanyLogo { get; set; }
    }

    public class GenerateLabelRequest
    {
        public Guid OrderId { get; set; }
        public string ServiceId { get; set; } = string.Empty; 
        public string AgencyId { get; set; } = string.Empty;
    }
}
