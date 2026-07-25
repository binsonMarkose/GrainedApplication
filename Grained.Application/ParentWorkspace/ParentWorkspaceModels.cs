namespace Grained.Application.ParentWorkspace;

// What a parent sees for each of their children.

public record ParentLessonDto(
    Guid Id,
    string Title,
    string BibleReference,
    string? Theme,
    string? MemoryVerseReference,
    string? MemoryVerseText,
    // "Completed" (your child did it), "Missed" (taught to the class, your child wasn't marked present),
    // or "Upcoming" (not taught to the class yet).
    string Status,
    DateTime? CompletedAtUtc);

public record ParentBadgeDto(
    Guid BadgeId,
    string Name,
    string? Description,
    string? IconName,
    DateTime AwardedAtUtc, // most recent award
    int Count);

public record ParentLessonDetailDto(
    Guid Id,
    string Title,
    string BibleReference,
    string? Theme,
    string StoryContent,
    string? LearningObjective,
    string? Activity,
    string? Prayer,
    string? MemoryVerseReference,
    string? MemoryVerseText);

public record ParentChildDto(
    Guid Id,
    string FirstName,
    string LastName,
    int Age,
    string ClassGroupName,
    string? AvatarId,
    Grained.Application.Growth.GrowthSummaryDto Growth,
    int CompletedCount,
    int MissedCount,
    int UpcomingCount,
    int TotalLessons,
    List<ParentLessonDto> Lessons,
    List<ParentBadgeDto> Badges);

public record ParentWorkspaceDto(
    string ParentName,
    List<ParentChildDto> Children);
