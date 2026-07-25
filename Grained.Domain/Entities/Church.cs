using Grained.Domain.Common;
using Grained.Domain.Enums;

namespace Grained.Domain.Entities;

public class Church : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }

    // Onboarding lifecycle. Defaults to Active for normal/seeded churches; the onboarding flow
    // creates churches as Pending until their admin accepts the invite. (IsActive stays as the
    // separate "disabled / soft-deleted" flag used across the app.)
    public ChurchStatus Status { get; set; } = ChurchStatus.Active;
    public string? Slug { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }

    public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();
    public ICollection<ClassGroup> ClassGroups { get; set; } = new List<ClassGroup>();
    public ICollection<Child> Children { get; set; } = new List<Child>();
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Badge> Badges { get; set; } = new List<Badge>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Campaign> Campaigns { get; set; } = new List<Campaign>();
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}
