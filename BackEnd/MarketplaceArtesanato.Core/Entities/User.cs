using MarketplaceArtesanato.Core.Entities.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketplaceArtesanato.Core.Entities;

[Table("Users")]
public class User : BaseEntity
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;
    public string CPF { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }

    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsApproved { get; set; } = true;

    public Seller? SellerProfile { get; set; }
    public Customer? CustomerProfile { get; set; }
    public Admin? AdminProfile { get; set; }

    public Guid? AddressId { get; set; }
    public Address? Address { get; set; }

    public List<Order> OrdersAsBuyer { get; set; } = new();
}