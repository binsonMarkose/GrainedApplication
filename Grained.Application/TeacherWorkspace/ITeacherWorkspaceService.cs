namespace Grained.Application.TeacherWorkspace;

public interface ITeacherWorkspaceService
{
    // Scoped to the signed-in teacher (by their ApplicationUser id) within their church.
    // Returns their assigned classes with published lessons + children. Empty if the user
    // has no teacher profile in this church.
    Task<TeacherWorkspaceDto> GetWorkspaceAsync(Guid userId, Guid churchId, CancellationToken ct = default);

    // Marks a lesson completed for every child who was present in the class on the given date, and
    // records the memory verse as learned for the children in verseChildIds. Returns how many
    // children the completion was recorded for.
    Task<int> MarkLessonCompletedAsync(Guid churchId, Guid lessonId, Guid classGroupId, DateOnly date, IReadOnlyList<Guid> verseChildIds, CancellationToken ct = default);

    // The active badges a teacher can award.
    Task<List<TeacherBadgeDto>> GetBadgeCatalogAsync(Guid churchId, CancellationToken ct = default);

    // Awards a badge to a child (must be a child in one of the teacher's classes).
    Task AwardBadgeAsync(Guid churchId, Guid childId, Guid badgeId, CancellationToken ct = default);
}
