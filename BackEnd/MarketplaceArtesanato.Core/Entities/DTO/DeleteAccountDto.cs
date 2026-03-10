using System.ComponentModel.DataAnnotations;

namespace MarketplaceArtesanato.Core.Models.Requests
{
    public class DeleteAccountDto
    {
        [Required(ErrorMessage = "A senha é obrigatória para deletar a conta.")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 255 caracteres.")]
        public string Password { get; set; } = string.Empty;
    }
}
