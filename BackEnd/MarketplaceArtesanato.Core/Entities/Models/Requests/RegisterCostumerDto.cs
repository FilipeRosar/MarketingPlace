using MarketplaceArtesanato.API.Models.Responses;
using MarketplaceArtesanato.Core.Entities.DTO;
using MarketplaceArtesanato.Core.Validators;
using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.API.Models.Requests
{
    public class RegisterCostumerDto
    {
        [Required(ErrorMessage ="O nome é obrigatorio")] 
        public string Name { get; set; } = string.Empty;
        [Required(ErrorMessage = "O e-mail é obrigatório"), EmailAddress(ErrorMessage = "E-mail inválido")] 
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage ="A senha é obrigatoria"), MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres")]
        public string Password { get; set; } = string.Empty;
        [Required, CPF]
        public string CPF { get; set; } = string.Empty;
        [Required]
        public AddressResponseDto Address { get; set; } = new();

        [Required(ErrorMessage = "O telefone é obrigatório")]
        public string Phone { get; set; } = string.Empty;
    }
}
