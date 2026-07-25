namespace Grained.Application.Growth;

public record GrowthForestEntryDto(
    string SeasonName,
    int StageIndex,
    string StageName,
    string StageEmoji,
    int GrowthPoints);

public record GrowthSummaryDto(
    string SeasonName,
    int StageIndex,
    string StageName,
    string StageEmoji,
    int GrowthPoints,
    int StageFloor, // GP where the current stage begins
    int? NextStageAt, // GP needed for the next stage (null at Harvest)
    string? NextStageName,
    // Breakdown of what earned this season's points
    int LessonsCompleted,
    int SundaysAttended,
    int VersesLearned,
    int BadgeCount,
    int AchievementCount,
    List<GrowthForestEntryDto> Forest);

public record GrowthSeasonDto(
    Guid Id,
    string Name,
    DateTime StartsOnUtc,
    DateTime EndsOnUtc,
    int Weeks, // ministry weeks in the window
    int HarvestPoints, // GP a faithful child reaches at season end (weeks × 12)
    bool IsCurrent);

// Admin form for creating/editing a season's ministry-year window.
public record GrowthSeasonFormModel(string? Name, DateTime StartsOnUtc, DateTime EndsOnUtc);

// Compact current-stage summary for one child (for admin/teacher list views).
public record ChildStageDto(Guid ChildId, int StageIndex, string StageName, string StageEmoji, int GrowthPoints);
