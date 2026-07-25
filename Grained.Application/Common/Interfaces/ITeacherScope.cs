namespace Grained.Application.Common.Interfaces;

// Resolves the visibility scope of the current user within a church.
// Returns null when the caller is unrestricted (ChurchAdmin / SuperAdmin, or not a teacher),
// or the list of class-group ids a "pure" Teacher is assigned to (empty list = sees nothing).
public interface ITeacherScope
{
    Task<List<Guid>?> GetAssignedClassGroupIdsAsync(Guid churchId, CancellationToken ct = default);
}
