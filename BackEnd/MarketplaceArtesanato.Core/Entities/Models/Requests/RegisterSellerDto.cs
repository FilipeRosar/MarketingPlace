using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities.DTO;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.API.Models.Requests
{
    public class RegisterSellerDto
    {
        [Required] 
        public string Name { get; set; } = string.Empty;
        [Required, EmailAddress] 
        public string Email { get; set; } = string.Empty;
        [Required, MinLength(8)] 
        public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? CPF { get; set; }
        public string? CNPJ { get; set; }
        [Required]
        public AddressDto Address { get; set; } = new();
    }

}
