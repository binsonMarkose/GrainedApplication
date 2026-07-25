namespace Grained.Application.Reports;

public interface IReportService
{
    Task<List<ChildProgressReportRow>> GetChildProgressReportAsync(Guid churchId, CancellationToken ct = default);
    Task<List<ChildBadgeReportRow>> GetChildBadgesAsync(Guid childId, Guid churchId, CancellationToken ct = default);
    Task<List<ClassProgressReportRow>> GetClassProgressReportAsync(Guid churchId, CancellationToken ct = default);
    Task<List<AttendanceReportRow>> GetAttendanceReportAsync(Guid churchId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<List<LessonCompletionReportRow>> GetLessonCompletionReportAsync(Guid churchId, CancellationToken ct = default);
}
