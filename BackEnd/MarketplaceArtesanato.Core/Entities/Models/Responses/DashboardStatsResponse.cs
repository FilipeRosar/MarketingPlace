using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.Models.Responses
{
    public class DashboardStatsResponse
    {
        public decimal TotalGMV { get; set; }        
        public int TotalOrders { get; set; }         
        public int NewUsersLastMonth { get; set; }   
        public decimal PlatformRevenue { get; set; }
        public int PendingApprovals { get; set; }
    }
}
