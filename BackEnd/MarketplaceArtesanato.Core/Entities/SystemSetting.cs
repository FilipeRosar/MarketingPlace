using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities
{
    [Table("SystemSettings")]
    public class SystemSetting : BaseEntity
    {
        [Required]
        public string Key { get; set; } = string.Empty; 

        [Required]
        public string Value { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty; 
    }
}
