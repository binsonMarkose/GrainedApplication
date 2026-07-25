namespace Grained.Application.Reports;

public record ChildProgressReportRow(
    Guid ChildId,
    string ChildName,
    string? AvatarId,
    string ClassGroupName,
    // Growth journey (current season)
    int StageIndex,
    string StageName,
    string StageEmoji,
    int GrowthPoints,
    int LessonsCompleted,
    int VersesLearned,
    int SundaysAttended,
    int BadgeCount,
    int AchievementCount,
    double? AverageQuizScore);

// A single badge/achievement a child has been awarded (report drill-down).
public record ChildBadgeReportRow(
    Guid BadgeId,
    string Name,
    string? Description,
    string? IconName,
    int Tier, // 0 = badge, 1 = achievement
    int Points,
    DateTime AwardedAtUtc);

public record ClassProgressReportRow(
    Guid ClassGroupId,
    string ClassGroupName,
    int TotalChildren,
    int TotalLessonsCompleted,
    double AverageCompletionRate);

public record AttendanceReportRow(
    Guid ClassGroupId,
    string ClassGroupName,
    int TotalSessions,
    int TotalPresent,
    int TotalAbsent,
    double AttendanceRatePercent);

public record LessonCompletionReportRow(
    Guid LessonId,
    string Title,
    bool IsPublished,
    int CompletedCount,
    double? AverageQuizScore);
