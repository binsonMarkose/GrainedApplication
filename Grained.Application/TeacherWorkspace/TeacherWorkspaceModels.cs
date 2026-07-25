namespace Grained.Application.TeacherWorkspace;

// The teacher's own scoped view: only the classes they're assigned to, and for each class
// the published lessons assigned to it plus the children in it.

public record TeacherWorkspaceBadgeDto(
    Guid BadgeId,
    string Name,
    string? IconName,
    int Count);

public record TeacherWorkspaceChildDto(
    Guid Id,
    string FirstName,
    string LastName,
    int Age,
    string? AvatarId,
    List<TeacherWorkspaceBadgeDto> Badges);

public record TeacherWorkspaceLessonDto(
    Guid Id,
    string Title,
    string BibleReference,
    string? Theme,
    string? MemoryVerseReference,
    // How many children in this class have completed this lesson (out of the class's children).
    int CompletedCount);

public record TeacherWorkspaceClassDto(
    Guid ClassGroupId,
    string Name,
    int MinAge,
    int MaxAge,
    string? Description,
    List<TeacherWorkspaceLessonDto> Lessons,
    List<TeacherWorkspaceChildDto> Children);

public record TeacherWorkspaceDto(
    string TeacherName,
    List<TeacherWorkspaceClassDto> Classes);

// The active badge catalog a teacher can award from.
public record TeacherBadgeDto(
    Guid Id,
    string Name,
    string? Description,
    string? IconName);
