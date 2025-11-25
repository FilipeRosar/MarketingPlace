using MarketplaceArtesanato.Core.Entities.DTO;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.Core.Models.Requests
{
    public class UpdateUserDto
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O telefone é obrigatório.")]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public AddressDto Address { get; set; } = new();
    }
}