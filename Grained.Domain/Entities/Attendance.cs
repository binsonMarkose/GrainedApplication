namespace Grained.Domain.Entities;

public class Attendance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChildId { get; set; }
    public Child Child { get; set; } = null!;

    public Guid ClassGroupId { get; set; }
    public ClassGroup ClassGroup { get; set; } = null!;

    public Guid? LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    public DateOnly AttendanceDate { get; set; }
    public bool IsPresent { get; set; }
    public string? Notes { get; set; }
}
