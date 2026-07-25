namespace Grained.Application.AttendanceTracking;

public interface IAttendanceService
{
    Task<List<AttendanceRosterEntryDto>> GetRosterAsync(Guid churchId, Guid classGroupId, DateOnly date, CancellationToken ct = default);
    Task SaveAsync(Guid churchId, AttendanceSaveModel model, CancellationToken ct = default);
}
