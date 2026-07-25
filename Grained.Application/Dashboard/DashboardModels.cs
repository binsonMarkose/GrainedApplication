namespace Grained.Application.Dashboard;

public record RecentAttendanceDto(string ChildName, string ClassGroupName, DateOnly AttendanceDate, bool IsPresent);

public record RecentCompletionDto(string ChildName, string LessonTitle, DateTime CompletedAtUtc);

public record DashboardSummaryDto(
    int TotalChildren,
    int TotalTeachers,
    int TotalClasses,
    int PublishedLessons,
    List<RecentAttendanceDto> RecentAttendance,
    List<RecentCompletionDto> RecentLessonCompletions);
