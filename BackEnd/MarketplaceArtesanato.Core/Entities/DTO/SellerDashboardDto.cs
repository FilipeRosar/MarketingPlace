using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class SellerDashboardDto
    {
        public Guid SellerId { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalSales { get; set; }
        public int ActiveProducts { get; set; }
        public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
    }
}
