namespace Grained.Application.ClassGroups;

public interface IClassGroupService
{
    Task<List<ClassGroupDto>> GetAllForChurchAsync(Guid churchId, bool includeInactive = false, CancellationToken ct = default);
    Task<ClassGroupDto?> GetByIdAsync(Guid id, Guid churchId, CancellationToken ct = default);
    Task<Guid> CreateAsync(Guid churchId, ClassGroupFormModel model, CancellationToken ct = default);
    Task UpdateAsync(Guid churchId, ClassGroupFormModel model, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, Guid churchId, bool isActive, CancellationToken ct = default);

    // Permanently removes an empty class group. Throws if children or attendance history is linked
    // (disable it instead in that case).
    Task DeleteAsync(Guid id, Guid churchId, CancellationToken ct = default);
}
