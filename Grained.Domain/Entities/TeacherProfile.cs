using Grained.Domain.Common;

namespace Grained.Domain.Entities;

public class TeacherProfile : AuditableEntity
{
    public Guid ApplicationUserId { get; set; }
    public ApplicationUser ApplicationUser { get; set; } = null!;

    public Guid ChurchId { get; set; }
    public Church Church { get; set; } = null!;

    public string? Bio { get; set; }

    public ICollection<TeacherClassGroup> AssignedClassGroups { get; set; } = new List<TeacherClassGroup>();
}
