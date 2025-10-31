using MarketplaceArtesanato.API.Models.Responses;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.API.Models.Requests
{
    public class RegisterSellerDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string CPF { get; set; }
        public string Phone { get; set; }
        public AddressResponseDto Address { get; set; }
    }
  
}
