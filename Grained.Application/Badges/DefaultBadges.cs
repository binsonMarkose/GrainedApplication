using Grained.Domain.Entities;
using Grained.Domain.Enums;

namespace Grained.Application.Badges;

// The starter badge set every new church is provisioned with, so admins can start awarding on day
// one. Icons are emoji (the app renders the raw string inside a gold medallion). Points follow the
// app convention: Standard = 12 (a faithful Sunday), Achievement = 36. Admins can edit, disable,
// add to, delete or award any of these freely afterwards.
public static class DefaultBadges
{
    public record Def(string Name, string Description, string Icon, string Criteria, BadgeTier Tier, int Points);

    public static readonly IReadOnlyList<Def> Catalog =
    [
        new("Little Disciple", "Took their first steps in faith.", "🌱", "Complete your first lesson", BadgeTier.Standard, 12),
        new("Bible Explorer", "Loves digging into God's Word.", "📖", "Complete 5 lessons", BadgeTier.Standard, 12),
        new("Prayer Warrior", "Faithful in prayer each week.", "🙏", "Pray each week for a month", BadgeTier.Standard, 12),
        new("Memory Master", "Hides God's Word in their heart.", "🧠", "Learn 5 memory verses", BadgeTier.Standard, 12),
        new("Kind Heart", "Shows Jesus' love to others.", "❤️", "Teacher nomination for kindness", BadgeTier.Standard, 12),
        new("Faithful Friend", "A caring friend to everyone.", "🤝", "Teacher nomination", BadgeTier.Standard, 12),
        new("Joyful Worshipper", "Worships God with gladness.", "🎵", "Join in worship", BadgeTier.Standard, 12),
        new("Shining Light", "Lets their light shine for Jesus.", "🌟", "A great attitude that lifts others", BadgeTier.Standard, 12),
        new("Scripture Scholar", "Memorised a whole passage or book.", "🏆", "Recite a full passage or book", BadgeTier.Achievement, 36),
        new("Baptised", "Publicly declared their faith.", "🕊️", "Baptism milestone", BadgeTier.Achievement, 36),
    ];

    // Fresh Badge rows for the given church (not yet added to a context).
    public static IEnumerable<Badge> ForChurch(Guid churchId) =>
        Catalog.Select(d => new Badge
        {
            ChurchId = churchId,
            Name = d.Name,
            Description = d.Description,
            IconName = d.Icon,
            Criteria = d.Criteria,
            Tier = d.Tier,
            Points = d.Points,
            // Effort/character (Standard) badges can be earned again and again; milestones can't.
            Repeatable = d.Tier == BadgeTier.Standard,
        });
}
