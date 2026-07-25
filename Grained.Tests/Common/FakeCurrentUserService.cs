using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;

namespace Grained.Tests.Common;

// Minimal ICurrentUserService for service tests. Defaults to a ChurchAdmin so that
// role-gated guards (e.g. teacher class-group scoping) are bypassed unless a test opts in.
public class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; init; }
    public Guid? ChurchId { get; init; }
    public bool IsSuperAdmin { get; init; }
    public bool IsChurchAdmin { get; init; } = true;
    public bool IsTeacher { get; init; }
    public bool IsParent { get; init; }

    public Guid RequireChurchId() =>
        ChurchId ?? throw new ValidationException("No church is associated with the current user.");
}
