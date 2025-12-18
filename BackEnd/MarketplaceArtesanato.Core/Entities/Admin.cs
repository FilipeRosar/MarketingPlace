using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities
{
    [Table("Admins")]
    public class Admin : BaseEntity
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        public string InternalCode { get; set; } = string.Empty; 

        public string Department { get; set; } = "General";

        public int AccessLevel { get; set; } = 1;

        public DateTime? LastLoginAt { get; set; }

        //public List<AuditLog> AuditLogs { get; set; } 
    }
}
