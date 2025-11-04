

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketplaceArtesanato.Core.Entities
{
    [Table("Ratings")]
    public class Rating
    {
        [Key]
        public int Id { get; set; }
        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        [Range(1, 5)]
        public int Stars { get; set; }

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [StringLength(500)]
        public string Review { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}