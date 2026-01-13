using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities
{
    [Table("UserFavorites")]
    public class UserFavorite : BaseEntity
    {
        public Guid UserId { get; set; }
        public User? User { get; set; }

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
