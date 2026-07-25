namespace Grained.Application.AttendanceTracking;

public record AttendanceRosterEntryDto(
    Guid ChildId,
    string FirstName,
    string LastName,
    bool IsPresent,
    string? Notes);

public class AttendanceEntryFormModel
{
    public Guid ChildId { get; set; }
    public bool IsPresent { get; set; }
    public string? Notes { get; set; }
}

public class AttendanceSaveModel
{
    public Guid ClassGroupId { get; set; }
    public DateOnly AttendanceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Guid? LessonId { get; set; }
    public List<AttendanceEntryFormModel> Entries { get; set; } = [];
}
