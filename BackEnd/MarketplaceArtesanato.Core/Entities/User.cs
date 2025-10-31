using MarketplaceArtesanato.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength(8)]
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; } = UserRole.Customer;
        public string? Phone { get; set; }
        [Required]
        public string CPF { get; set; }
        public Guid AddressId { get; set; }
        public Address Address { get; set; }
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
    public class Address
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Street { get; set; }
        [Required]
        public string Number { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
        [Required]
        public string ZipCode { get; set; }
        [Required]
        public string Country { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

    }
}
