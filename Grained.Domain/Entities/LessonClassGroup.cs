namespace Grained.Domain.Entities;

// Join entity: which class groups a lesson has been assigned to.
public class LessonClassGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public Guid ClassGroupId { get; set; }
    public ClassGroup ClassGroup { get; set; } = null!;

    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    // Teaching order within this class group (0-based). Admins reorder the curriculum per group;
    // teachers/parents see lessons in this sequence.
    public int SortOrder { get; set; }
}
