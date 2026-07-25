namespace Grained.Domain.Entities;

public class ChildBadge
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChildId { get; set; }
    public Child Child { get; set; } = null!;

    public Guid BadgeId { get; set; }
    public Badge Badge { get; set; } = null!;

    public DateTime AwardedAtUtc { get; set; } = DateTime.UtcNow;
}
