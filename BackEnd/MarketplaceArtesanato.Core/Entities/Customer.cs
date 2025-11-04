using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities
{
    [Table("Customer")]
    public class Customer
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required] 
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;

        public Guid AddressId { get; set; }
        public Address Address { get; set; } = null!;

        public List<Order> Orders { get; set; } = new();
        public List<Rating> Ratings { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
