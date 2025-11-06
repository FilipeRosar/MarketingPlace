using MarketplaceArtesanato.Core.Events;

public class CheckoutInitiatedEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public string StripeSessionId { get; set; } = string.Empty;
    public List<CheckoutItemEvent> Items { get; set; } = new();
    public decimal Total { get; set; }
    public DateTime InitiatedAt { get; set; }
}