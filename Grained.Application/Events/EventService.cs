using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Events;

public class EventService(IApplicationDbContext db) : IEventService
{
    public async Task<List<EventListItemDto>> GetForChurchAsync(Guid churchId, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = db.Events.AsNoTracking().Where(e => e.ChurchId == churchId);
        if (!includeInactive)
            query = query.Where(e => e.IsActive);

        return await query
            .OrderByDescending(e => e.StartDate)
            .Select(e => new EventListItemDto(
                e.Id, e.ChurchId, e.Title, e.StartDate, e.EndDate, e.Location,
                e.EnableTshirt, e.IsPublished, e.IsActive, e.TicketTypes.Count))
            .ToListAsync(ct);
    }

    public async Task<EventDetailDto?> GetDetailAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var ev = await db.Events.AsNoTracking()
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id && e.ChurchId == churchId, ct);

        return ev is null ? null : ToDetailDto(ev);
    }

    public async Task<Guid> CreateAsync(Guid churchId, EventFormModel model, CancellationToken ct = default)
    {
        var ev = new Event
        {
            ChurchId = churchId,
            Title = model.Title.Trim(),
            StartDate = Normalize(model.StartDate),
            EndDate = Normalize(model.EndDate),
            Location = model.Location?.Trim(),
            Description = model.Description?.Trim(),
            EnableTshirt = model.EnableTshirt
        };

        var order = 0;
        foreach (var t in model.TicketTypes)
            ev.TicketTypes.Add(new EventTicketType { Name = t.Name.Trim(), Price = t.Price, SortOrder = order++ });

        db.Events.Add(ev);
        await db.SaveChangesAsync(ct);
        return ev.Id;
    }

    public async Task UpdateAsync(Guid churchId, EventFormModel model, CancellationToken ct = default)
    {
        if (model.Id is null)
            throw new ValidationException("Event id is required for update.");

        var ev = await db.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == model.Id && e.ChurchId == churchId, ct)
            ?? throw new ValidationException("Event not found.");

        ev.Title = model.Title.Trim();
        ev.StartDate = Normalize(model.StartDate);
        ev.EndDate = Normalize(model.EndDate);
        ev.Location = model.Location?.Trim();
        ev.Description = model.Description?.Trim();
        ev.EnableTshirt = model.EnableTshirt;
        ev.UpdatedAtUtc = DateTime.UtcNow;

        // Sync ticket types: remove dropped rows, update matched, insert new.
        var keepIds = model.TicketTypes.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToHashSet();
        foreach (var removed in ev.TicketTypes.Where(t => !keepIds.Contains(t.Id)).ToList())
            db.EventTicketTypes.Remove(removed);

        var order = 0;
        foreach (var t in model.TicketTypes)
        {
            var existing = t.Id.HasValue ? ev.TicketTypes.FirstOrDefault(x => x.Id == t.Id.Value) : null;
            if (existing is null)
                ev.TicketTypes.Add(new EventTicketType { Name = t.Name.Trim(), Price = t.Price, SortOrder = order });
            else
            {
                existing.Name = t.Name.Trim();
                existing.Price = t.Price;
                existing.SortOrder = order;
            }
            order++;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task PublishAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var ev = await db.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.Id == id && e.ChurchId == churchId, ct)
            ?? throw new ValidationException("Event not found.");

        if (ev.TicketTypes.Count == 0)
            throw new ValidationException("Add at least one ticket type before publishing.");

        ev.IsPublished = true;
        ev.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task UnpublishAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id && e.ChurchId == churchId, ct)
            ?? throw new ValidationException("Event not found.");
        ev.IsPublished = false;
        ev.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(Guid id, Guid churchId, bool isActive, CancellationToken ct = default)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id && e.ChurchId == churchId, ct)
            ?? throw new ValidationException("Event not found.");
        ev.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var ev = await db.Events.FirstOrDefaultAsync(e => e.Id == id && e.ChurchId == churchId, ct)
            ?? throw new ValidationException("Event not found.");

        // Registrations are a Restrict FK (and hold booking/payment history) — never silently drop
        // them. Ticket types cascade with the event.
        if (await db.EventRegistrations.AnyAsync(r => r.EventId == id, ct))
            throw new ValidationException("This event has registrations and can’t be deleted. Disable it instead.");

        db.Events.Remove(ev);
        await db.SaveChangesAsync(ct);
    }

    // The event form supplies wall-clock start/end times with no zone; Npgsql's timestamptz
    // columns require UTC-kind DateTimes, so we treat the entered value as UTC.
    private static DateTime Normalize(DateTime d) => DateTime.SpecifyKind(d, DateTimeKind.Utc);

    private static EventDetailDto ToDetailDto(Event e) => new(
        e.Id, e.ChurchId, e.Title, e.StartDate, e.EndDate, e.Location, e.Description,
        e.EnableTshirt, e.IsPublished, e.IsActive,
        e.TicketTypes.OrderBy(t => t.SortOrder)
            .Select(t => new EventTicketTypeDto(t.Id, t.Name, t.Price)).ToList());
}
