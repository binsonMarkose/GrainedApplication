namespace Grained.Application.Events;

public interface IEventService
{
    Task<List<EventListItemDto>> GetForChurchAsync(Guid churchId, bool includeInactive = false, CancellationToken ct = default);
    Task<EventDetailDto?> GetDetailAsync(Guid id, Guid churchId, CancellationToken ct = default);
    Task<Guid> CreateAsync(Guid churchId, EventFormModel model, CancellationToken ct = default);
    Task UpdateAsync(Guid churchId, EventFormModel model, CancellationToken ct = default);
    Task PublishAsync(Guid id, Guid churchId, CancellationToken ct = default);
    Task UnpublishAsync(Guid id, Guid churchId, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, Guid churchId, bool isActive, CancellationToken ct = default);

    // Permanently removes an event (and its ticket types). Throws if it has registrations — disable
    // it instead in that case.
    Task DeleteAsync(Guid id, Guid churchId, CancellationToken ct = default);
}
