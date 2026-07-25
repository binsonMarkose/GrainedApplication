using Grained.Domain.Common;

namespace Grained.Domain.Entities;

public class Child : AuditableEntity
{
    public Guid ChurchId { get; set; }
    public Church Church { get; set; } = null!;

    public Guid ClassGroupId { get; set; }
    public ClassGroup ClassGroup { get; set; } = null!;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }

    // A fun avatar the child picks (a key into the front-end avatar catalog, e.g. "fox").
    public string? AvatarId { get; set; }

    public string ParentName { get; set; } = string.Empty;
    public string ParentEmail { get; set; } = string.Empty;
    public string? ParentPhone { get; set; }

    // Set once a parent login code is generated: links this child to the parent's account
    // (siblings sharing a ParentEmail all point to the same parent user).
    public Guid? ParentUserId { get; set; }
    public ApplicationUser? ParentUser { get; set; }

    public ICollection<ChildProgress> ChildProgresses { get; set; } = new List<ChildProgress>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<ChildBadge> ChildBadges { get; set; } = new List<ChildBadge>();
}
