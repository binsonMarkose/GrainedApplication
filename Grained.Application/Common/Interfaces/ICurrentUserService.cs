namespace Grained.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    Guid? ChurchId { get; }
    bool IsSuperAdmin { get; }
    bool IsChurchAdmin { get; }
    bool IsTeacher { get; }
    bool IsParent { get; }

    // Throws if the current user has no church context (e.g. a SuperAdmin acting without one selected).
    Guid RequireChurchId();
}
