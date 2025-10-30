using MarketplaceArtesanato.Core.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public List<string> Images { get; set; } = new List<string>();
        public ProductCategory Category { get; set; } 
        public string Status { get; set; }
        public Ratings Ratings { get; set; } = new Ratings();
        public Guid SellerId { get; set; }
        public User Seller { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
    public class Ratings
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Stars { get; set; }
        public string Review { get; set; } = string.Empty;
    }
}
