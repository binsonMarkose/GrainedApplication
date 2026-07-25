namespace Grained.Domain.Entities;

// A public booking for an event: who booked, their ticket selections (as priced line snapshots),
// an optional T-shirt size, and the linked Payment.
public class EventRegistration
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;

    public Guid? PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public string PurchaserName { get; set; } = string.Empty;
    public string PurchaserEmail { get; set; } = string.Empty;
    public string? PurchaserPhone { get; set; }
    public string? TshirtSize { get; set; }

    public decimal Total { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<EventRegistrationLine> Lines { get; set; } = new List<EventRegistrationLine>();
}
