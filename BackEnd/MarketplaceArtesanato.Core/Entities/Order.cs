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

    [Column(TypeName = "decimal(18,2)")]
    public decimal ShippingCost { get; set; }

    public string? StripeSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string Carrier { get; set; } = string.Empty;

    public string? ShippingAddressJson { get; set; }

    [NotMapped]
    public ShippingAddress? ShippingAddress
    {
        get => string.IsNullOrEmpty(ShippingAddressJson)
            ? null
            : JsonSerializer.Deserialize<ShippingAddress>(ShippingAddressJson);
        set => ShippingAddressJson = value == null
            ? null
            : JsonSerializer.Serialize(value);
    }

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
        var itemsTotal = Items.Sum(i => i.UnitPrice * i.Quantity);
        TotalAmount = itemsTotal + ShippingCost;
    }

    [NotMapped]
    public decimal Subtotal => Items.Sum(i => i.UnitPrice * i.Quantity);
}

public class ShippingAddress
{
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}