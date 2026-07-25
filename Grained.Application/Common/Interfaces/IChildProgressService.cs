using Grained.Application.Common.Models;

namespace Grained.Application.Common.Interfaces;

public interface IChildProgressService
{
    Task<List<ChildProgressDto>> GetForChildAsync(Guid childId, Guid churchId, CancellationToken ct = default);
    Task UpsertAsync(Guid churchId, ChildProgressUpdateModel model, CancellationToken ct = default);
}
