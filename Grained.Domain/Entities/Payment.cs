using Grained.Domain.Enums;

namespace Grained.Domain.Entities;

// A single money movement, shared by event registrations and (later) donations. Structure-first:
// the "NoCard" dev provider records it as Paid immediately; Stripe drops in behind IPaymentGateway
// later (Provider = "Stripe", ProviderReference = the Stripe id, confirmed by webhook).
public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChurchId { get; set; }
    public Church Church { get; set; } = null!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";

    public string Provider { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string PayerName { get; set; } = string.Empty;
    public string PayerEmail { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAtUtc { get; set; }
}
