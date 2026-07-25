namespace Grained.Application.Growth;

public interface IGrowthService
{
    Task<List<GrowthSeasonDto>> ListSeasonsAsync(Guid churchId, CancellationToken ct = default);

    // Creates a season for an admin-defined ministry-year window (start→end); the running tree
    // completes and joins each child's forest, and everyone begins a fresh tree.
    Task<GrowthSeasonDto> CreateSeasonAsync(Guid churchId, GrowthSeasonFormModel model, CancellationToken ct = default);

    // Adjusts a season's name / start / end (e.g. to correct the ministry-year dates).
    Task<GrowthSeasonDto> UpdateSeasonAsync(Guid id, Guid churchId, GrowthSeasonFormModel model, CancellationToken ct = default);

    // Growth for a set of children (current stage + breakdown + forest of past seasons).
    Task<Dictionary<Guid, GrowthSummaryDto>> GetGrowthForChildrenAsync(
        IReadOnlyList<Guid> childIds, Guid churchId, CancellationToken ct = default);

    // Compact current stage for every active child in the church (list views).
    Task<List<ChildStageDto>> GetChildStagesAsync(Guid churchId, CancellationToken ct = default);
}
