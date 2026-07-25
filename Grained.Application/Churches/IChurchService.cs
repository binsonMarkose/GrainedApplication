namespace Grained.Application.Churches;

public interface IChurchService
{
    Task<List<ChurchDto>> GetAllAsync(bool includeInactive = false, Grained.Domain.Enums.ChurchStatus? status = null, CancellationToken ct = default);
    Task<ChurchDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateAsync(ChurchFormModel model, CancellationToken ct = default);
    Task UpdateAsync(ChurchFormModel model, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default);
}
