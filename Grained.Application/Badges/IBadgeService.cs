namespace Grained.Application.Badges;

public interface IBadgeService
{
    Task<List<BadgeDto>> GetForChurchAsync(Guid churchId, bool includeInactive = false, CancellationToken ct = default);
    Task<BadgeDto?> GetByIdAsync(Guid id, Guid churchId, CancellationToken ct = default);
    Task<Guid> CreateAsync(Guid churchId, BadgeFormModel model, CancellationToken ct = default);
    Task UpdateAsync(Guid churchId, BadgeFormModel model, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, Guid churchId, bool isActive, CancellationToken ct = default);

    // ChurchAdmin awards any badge (incl. Achievement tier) to a child.
    Task AwardToChildAsync(Guid churchId, Guid childId, Guid badgeId, CancellationToken ct = default);
}
