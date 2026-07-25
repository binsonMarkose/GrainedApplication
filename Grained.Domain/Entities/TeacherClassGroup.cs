namespace Grained.Domain.Entities;

// Join entity: which class groups a teacher is assigned to teach.
public class TeacherClassGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TeacherProfileId { get; set; }
    public TeacherProfile TeacherProfile { get; set; } = null!;

    public Guid ClassGroupId { get; set; }
    public ClassGroup ClassGroup { get; set; } = null!;
}
