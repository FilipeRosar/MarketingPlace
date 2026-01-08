using MarketplaceArtesanato.Core.Entities.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MarketplaceArtesanato.Core.Entities;

[Table("Orders")]
public class Order : BaseEntity
{
    public Guid BuyerId { get; set; }
    public User Buyer { get; set; } = null!;
    public DateTime ShippedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public string? StripeSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string Carrier { get; set; } = string.Empty;
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


    [NotMapped]
    public Dictionary<Guid, string> TrackingCodes { get; set; } = new();

    public string? TrackingCodesJson
    {
        get => TrackingCodes == null || !TrackingCodes.Any()
            ? null
            : JsonSerializer.Serialize(TrackingCodes);
        set => TrackingCodes = string.IsNullOrEmpty(value)
            ? new()
            : JsonSerializer.Deserialize<Dictionary<Guid, string>>(value)!;
    }

    public void CalculateTotal()
    {
        TotalAmount = Items.Sum(i => i.UnitPrice * i.Quantity);
    }
}