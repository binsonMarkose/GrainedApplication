using Grained.Domain.Common;

namespace Grained.Domain.Entities;

public class Campaign : AuditableEntity
{
    public Guid ChurchId { get; set; }
    public Church Church { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Optional fundraising goal. Raised amount is computed on read from paid donations, not stored.
    public decimal? TargetAmount { get; set; }

    // Points at a StoredImage (no hard FK, so deleting/replacing an image never blocks the campaign).
    public Guid? LogoImageId { get; set; }

    public bool IsPublished { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Donation> Donations { get; set; } = new List<Donation>();
}
