using Grained.Domain.Common;

namespace Grained.Domain.Entities;

public class ClassGroup : AuditableEntity
{
    public Guid ChurchId { get; set; }
    public Church Church { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public string? Description { get; set; }

    public ICollection<Child> Children { get; set; } = new List<Child>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<TeacherClassGroup> AssignedTeachers { get; set; } = new List<TeacherClassGroup>();
    public ICollection<LessonClassGroup> AssignedLessons { get; set; } = new List<LessonClassGroup>();
}
