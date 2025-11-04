
using MarketplaceArtesanato.Core.Validators;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketplaceArtesanato.Core.Entities
{
    [SellerDocument]
    [Table("Sellers")]
    public class Seller
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required, MinLength(8)]
        public string PasswordHash { get; set; } = string.Empty;
        public string? Phone { get; set; }
        [StringLength(14)]
        public string? CPF { get; set; }

        [StringLength(18)]
        public string? CNPJ { get; set; }
        [Required(ErrorMessage = "CPF ou CNPJ é obrigatório")]
        public string Document => CPF ?? CNPJ ?? throw new ValidationException("CPF ou CNPJ requerido");
        public Guid AddressId { get; set; }
        public Address Address { get; set; } = null!;

        public List<Product> Products { get; set; } = new();
        public List<Order> OrdersReceived { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

