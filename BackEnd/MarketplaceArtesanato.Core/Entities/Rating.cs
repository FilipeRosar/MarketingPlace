
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketplaceArtesanato.Core.Entities
{
    [Table("Ratings")]
    public class Rating : BaseEntity
    {
        public Guid CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        [Range(1, 5)]
        public int Stars { get; set; }

        [StringLength(500)]
        public string Review { get; set; } = string.Empty;

        [StringLength(500)]
        public string? SellerReply { get; set; }

        public DateTime? SellerReplyAt { get; set; }

    }
}
