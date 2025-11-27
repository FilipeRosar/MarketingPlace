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
    [PrimaryKey(nameof(UserId), nameof(ProductId))] // Chave composta
    public class UserFavorite : BaseEntity
    {
        public Guid UserId { get; set; }
        public Customer? User { get; set; } // Navegação opcional para Customer ou Seller

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
