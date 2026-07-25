using Grained.Domain.Enums;

namespace Grained.Application.Payments;

public record PaymentRequest(
    Guid ChurchId,
    decimal Amount,
    string Currency,
    string Description,
    string PayerName,
    string PayerEmail);

// CheckoutUrl is null for the dev "record payment" gateway (paid instantly); a real gateway
// (Stripe) returns a hosted Checkout URL and a Pending status, later confirmed via webhook.
public record PaymentResult(string Provider, string Reference, PaymentStatus Status, string? CheckoutUrl);

// The single seam every paid flow (event registration now, donations later) goes through.
public interface IPaymentGateway
{
    Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, CancellationToken ct = default);
}
