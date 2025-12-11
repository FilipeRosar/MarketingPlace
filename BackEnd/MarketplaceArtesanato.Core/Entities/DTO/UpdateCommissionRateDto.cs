using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class UpdateCommissionRateDto
    {
        public decimal Rate { get; set; }
    }

    public class UpdateServiceFeeDto
    {
        public decimal Fee { get; set; }
    }
}
