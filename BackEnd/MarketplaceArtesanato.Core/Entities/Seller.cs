using MarketplaceArtesanato.Core.Entities;
using MarketplaceArtesanato.Core.Validators;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace MarketplaceArtesanato.Core.Entities
{
    [SellerDocument]
    [Table("Sellers")]
    public class Seller : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public string StoreName { get; set; } = string.Empty; 

        [Required]
        public string StoreSlug { get; set; } = string.Empty;

        public string? Bio { get; set; }
        public string? ProfileImageUrl { get; set; } 
        public string? BannerImageUrl { get; set; } 

        [StringLength(18)]
        public string? CNPJ { get; set; } 

        public string? PixKey { get; set; } 

        public decimal CommissionRate { get; set; } = 15.0m;

        public Guid AddressId { get; set; }
        public Address Address { get; set; } = null!;

        public bool IsApproved { get; set; } = false; 
        public bool IsOnVacation { get; set; } = false;
        public string? StripeAccountId { get; set; }
        public bool IsStripeConnected { get; set; } = false;
        public decimal RatingAverage { get; set; } = 0m; 
        public int TotalSales { get; set; } = 0;
        public string? InstagramUrl { get; set; }
        public List<Product> Products { get; set; } = new();
        public List<Order> OrdersReceived { get; set; } = new();
    }
}