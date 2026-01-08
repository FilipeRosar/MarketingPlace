using System;

namespace MarketplaceArtesanato.Core.Entities.Models.Responses
{
    public class CommissionReportItemResponse
    {
        public Guid SellerId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public decimal TotalSales { get; set; }
        public decimal CommissionEarned { get; set; }
        public decimal Rate { get; set; }
    }
}
