using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketplaceArtesanato.Core.Entities.DTO
{
    public class CartDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public List<CartItemDto> Items { get; set; } = new();

        public decimal TotalPrice => Items.Sum(i => i.Subtotal);
        public decimal TotalDiscount => Items.Sum(i => i.TotalDiscount);
        public decimal OriginalTotal => Items.Sum(i => i.OriginalTotal);

        public int TotalProducts => Items.Count;
        public int TotalItems => Items.Sum(i => i.Quantity);

        public string TotalPriceFormatted => TotalPrice.ToString("C");
        public string TotalDiscountFormatted => TotalDiscount.ToString("C");

        public List<DiscountSummaryDto> DiscountSummary => Items
            .SelectMany(i => i.DiscountDetails.Select(d => new
            {
                d.Type,
                d.Description,
                Amount = d.Amount * i.Quantity
            }))
            .GroupBy(x => x.Type)
            .Select(g => new DiscountSummaryDto
            {
                Type = g.Key,
                Description = g.First().Description,
                TotalAmount = g.Sum(x => x.Amount)
            })
            .OrderByDescending(d => d.TotalAmount)
            .ToList();
    }
    public class DiscountSummaryDto
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

}
