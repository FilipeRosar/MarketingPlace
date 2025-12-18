using MarketplaceArtesanato.Core.Entities.Enums;
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
    public class Customer : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime? BirthDate { get; set; } 
        public bool NewsletterSubscribed { get; set; } = true;
        public int LoyaltyPoints { get; set; } = 0;
        public DateTime? LastPurchaseDate { get; set; }
        public List<Rating> Ratings { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<Product> FavoriteProducts { get; set; } = new(); 
    }
}
