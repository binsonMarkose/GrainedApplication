using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Application.Payments;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Public;

// Anonymous, public-facing read + register flow. Only ever exposes published, active events of
// active churches; never leaks admin fields.
public class PublicEventService(IApplicationDbContext db, IPaymentGateway gateway) : IPublicEventService
{
    public async Task<PublicChurchDto?> GetStorefrontAsync(string slug, CancellationToken ct = default)
    {
        var normalized = slug.Trim().ToLowerInvariant();

        var church = await db.Churches.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == normalized && c.IsActive && c.Status == ChurchStatus.Active, ct);
        if (church is null)
            return null;

        var events = await db.Events.AsNoTracking()
            .Where(e => e.ChurchId == church.Id && e.IsActive && e.IsPublished)
            .OrderBy(e => e.StartDate)
            .Select(e => new PublicEventListItemDto(
                e.Id, e.Title, e.StartDate, e.EndDate, e.Location,
                e.TicketTypes.Count == 0 ? (decimal?)null : e.TicketTypes.Min(t => t.Price)))
            .ToListAsync(ct);

        var campaigns = await db.Campaigns.AsNoTracking()
            .Where(c => c.ChurchId == church.Id && c.IsActive && c.IsPublished)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new PublicCampaignListItemDto(
                c.Id, c.Title, c.TargetAmount,
                c.Donations.Where(d => d.Payment != null && d.Payment.Status == PaymentStatus.Paid).Sum(d => d.Amount),
                c.LogoImageId))
            .ToListAsync(ct);

        return new PublicChurchDto(church.Slug!, church.Name, events, campaigns);
    }

    public async Task<PublicEventDto?> GetEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var ev = await db.Events.AsNoTracking()
            .Include(e => e.Church)
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.IsActive && e.IsPublished, ct);
        if (ev is null)
            return null;

        return new PublicEventDto(
            ev.Id, ev.Church.Name, ev.Title, ev.StartDate, ev.EndDate, ev.Location, ev.Description, ev.EnableTshirt,
            ev.TicketTypes.OrderBy(t => t.SortOrder)
                .Select(t => new PublicTicketTypeDto(t.Id, t.Name, t.Price)).ToList());
    }

    public async Task<RegistrationResultDto> RegisterAsync(Guid eventId, EventRegistrationModel model, CancellationToken ct = default)
    {
        var ev = await db.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.IsActive && e.IsPublished, ct)
            ?? throw new ValidationException("This event is not open for registration.");

        // Build priced line snapshots from the requested selections (ignoring zero quantities).
        var lines = new List<EventRegistrationLine>();
        decimal total = 0;
        foreach (var sel in model.Selections.Where(s => s.Quantity > 0))
        {
            var ticket = ev.TicketTypes.FirstOrDefault(t => t.Id == sel.TicketTypeId)
                ?? throw new ValidationException("One of the selected ticket types is not part of this event.");

            lines.Add(new EventRegistrationLine
            {
                EventTicketTypeId = ticket.Id,
                TicketTypeName = ticket.Name,
                Quantity = sel.Quantity,
                UnitPrice = ticket.Price
            });
            total += ticket.Price * sel.Quantity;
        }

        if (lines.Count == 0)
            throw new ValidationException("Select at least one ticket.");

        // Take payment via the seam (dev gateway records it as Paid immediately).
        var payResult = await gateway.CreatePaymentAsync(
            new PaymentRequest(ev.ChurchId, total, "GBP", $"Registration: {ev.Title}", model.PurchaserName.Trim(), model.PurchaserEmail.Trim()),
            ct);

        var payment = new Payment
        {
            ChurchId = ev.ChurchId,
            Amount = total,
            Currency = "GBP",
            Provider = payResult.Provider,
            ProviderReference = payResult.Reference,
            Status = payResult.Status,
            PayerName = model.PurchaserName.Trim(),
            PayerEmail = model.PurchaserEmail.Trim(),
            PaidAtUtc = payResult.Status == PaymentStatus.Paid ? DateTime.UtcNow : null
        };
        db.Payments.Add(payment);

        var registration = new EventRegistration
        {
            EventId = ev.Id,
            PaymentId = payment.Id,
            PurchaserName = model.PurchaserName.Trim(),
            PurchaserEmail = model.PurchaserEmail.Trim(),
            PurchaserPhone = model.PurchaserPhone?.Trim(),
            TshirtSize = ev.EnableTshirt ? model.TshirtSize?.Trim() : null,
            Total = total,
            Lines = lines
        };
        db.EventRegistrations.Add(registration);

        await db.SaveChangesAsync(ct);

        return new RegistrationResultDto(registration.Id, total, "GBP", payment.Status.ToString(), payResult.Reference);
    }
}
