namespace Grained.Application.ParentWorkspace;

public interface IParentWorkspaceService
{
    // Scoped to the signed-in parent (by their ApplicationUser id): their linked children,
    // each child's lesson progress (completed / missed / upcoming) and earned badges.
    Task<ParentWorkspaceDto> GetForParentAsync(Guid userId, Guid churchId, CancellationToken ct = default);

    // Sets the chosen avatar for one of the parent's children.
    Task SetChildAvatarAsync(Guid userId, Guid churchId, Guid childId, string? avatarId, CancellationToken ct = default);

    // Full lesson content for revisiting at home — only lessons in the parent's children's classes.
    Task<ParentLessonDetailDto> GetLessonAsync(Guid userId, Guid churchId, Guid lessonId, CancellationToken ct = default);
}
