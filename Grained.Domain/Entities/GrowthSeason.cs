namespace Grained.Domain.Entities;

// A ministry "year" / season. Growth points accrue within the current season; when a new season
// starts, the previous tree is complete and joins the child's "forest" of past years.
// Seasons are just date boundaries — a child's growth in any season is computed from the timestamps
// of their lessons, attendance and badges that fall inside the season window.
public class GrowthSeason
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChurchId { get; set; }
    public Church Church { get; set; } = null!;

    public string Name { get; set; } = string.Empty; // e.g. "2026" or "2026–27"

    // The ministry-year window, set by the ChurchAdmin. The growth-path targets scale to its length
    // so a faithful child reaches Harvest at EndsOnUtc regardless of how long the church's year is.
    public DateTime StartsOnUtc { get; set; }
    public DateTime EndsOnUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
