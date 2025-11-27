using MarketplaceArtesanato.Core.Events;

public class CheckoutInitiatedEvent
{
    public Guid CustomerId { get; set; }
    public string StripeSessionId { get; set; } = string.Empty;
    public List<CheckoutItemEvent> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime InitiatedAt { get; set; }
}

public class CheckoutItemEvent
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}