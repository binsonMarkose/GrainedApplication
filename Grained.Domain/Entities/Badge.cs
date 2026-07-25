using Grained.Domain.Common;
using Grained.Domain.Enums;

namespace Grained.Domain.Entities;

public class Badge : AuditableEntity
{
    public Guid ChurchId { get; set; }
    public Church Church { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public string? Criteria { get; set; }

    // Standard (teacher) vs Achievement (admin milestone), and the growth points it's worth.
    public BadgeTier Tier { get; set; } = BadgeTier.Standard;
    public int Points { get; set; } = 12;

    // Milestones (Baptised, finished a unit) are awarded once; effort/character badges (Kind, Prayer)
    // can be earned again and again. Defaults align with tier: Standard repeatable, Achievement not.
    public bool Repeatable { get; set; }

    public ICollection<ChildBadge> ChildBadges { get; set; } = new List<ChildBadge>();
}
