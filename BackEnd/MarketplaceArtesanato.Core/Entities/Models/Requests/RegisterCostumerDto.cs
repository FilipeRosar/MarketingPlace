using MarketplaceArtesanato.API.Models.Responses;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.API.Models.Requests
{
    public class RegisterCostumerDto
    {
        [Required] 
        public string Name { get; set; } = string.Empty;
        [Required, EmailAddress] 
        public string Email { get; set; } = string.Empty;
        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
        [Required]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "CPF deve ter 11 dígitos.")]
        public string CPF { get; set; }
        public AddressResponseDto Address { get; set; }
        public string? Phone { get; set; }
    }
}
