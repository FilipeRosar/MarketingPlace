// Core/Entities/Order.cs
using MarketplaceArtesanato.Core.Entities.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MarketplaceArtesanato.Core.Entities;

[Table("Orders")]
public class Order : BaseEntity
{
    public Guid BuyerId { get; set; }
    public Customer Buyer { get; set; } = null!;

    public List<OrderItem> Items { get; set; } = new();

    public decimal TotalAmount { get; set; }
    public string? TrackingCode { get; set; } 
    public string? Carrier { get; set; }     
    public DateTime? ShippedAt { get; set; }
    public string? StripeSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [NotMapped]
    public Dictionary<Guid, decimal> SellerCommissions { get; set; } = new();

    public string? SellerCommissionsJson
    {
        get => SellerCommissions == null || !SellerCommissions.Any()
            ? null
            : JsonSerializer.Serialize(SellerCommissions);
        set => SellerCommissions = string.IsNullOrEmpty(value)
            ? new()
            : JsonSerializer.Deserialize<Dictionary<Guid, decimal>>(value)!;
    }

    public void CalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.UnitPrice * i.Quantity);
    }
}