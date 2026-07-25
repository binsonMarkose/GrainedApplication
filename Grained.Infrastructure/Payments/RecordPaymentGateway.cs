using Grained.Application.Payments;
using Grained.Domain.Enums;

namespace Grained.Infrastructure.Payments;

// Dev / structure-first gateway: records the payment as immediately Paid with a synthetic reference,
// so the whole register/donate flow works end-to-end with no card processor wired up. Swap for a
// StripePaymentGateway behind IPaymentGateway (config) with no change to the calling services.
public class RecordPaymentGateway : IPaymentGateway
{
    public Task<PaymentResult> CreatePaymentAsync(PaymentRequest request, CancellationToken ct = default) =>
        Task.FromResult(new PaymentResult("NoCard", $"NOCARD-{Guid.NewGuid():N}", PaymentStatus.Paid, null));
}
