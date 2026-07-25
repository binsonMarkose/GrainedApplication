namespace Grained.Application.Growth;

public record GrowthStage(int Index, string Name, string Emoji, int Gp);

// The single place the growth journey is tuned: the stage curve + the point weights.
//
// A "faithful Sunday" = the three weekly acts, each worth 4: came (attend) + learned the lesson
// + learned the memory verse = 12 points. A full faithful *ministry year* reaches Harvest — but a
// ministry year varies from church to church, so the targets are NOT fixed. They scale to the
// season length the church set (start→end): Harvest = weeks × 12, and the seven stages keep the
// same relative shape (a fixed curve of faithful-Sundays-out-of-52). Badge/achievement points come
// from each Badge's own Points value (defaults: 12 standard, 36 achievement).
public static class GrowthLevels
{
    public const int LessonPoints = 4; // learned the lesson (completed)
    public const int AttendancePoints = 4; // came to church (present)
    public const int VersePoints = 4; // learned the memory verse
    public const int WeeklyPoints = LessonPoints + AttendancePoints + VersePoints; // 12 per faithful Sunday

    // The stage curve, expressed as faithful Sundays out of a reference 52-week year. Scaled to the
    // actual season length below. (At 52 weeks this reproduces the original 0/60/144/…/624 targets.)
    private record StageDef(string Name, string Emoji, int Sundays);

    private static readonly StageDef[] Curve =
    {
        new("Seed", "🌰", 0),
        new("Roots", "🌱", 5),
        new("Sprout", "🌿", 12),
        new("Sapling", "🪴", 21),
        new("Tree", "🌳", 31),
        new("Fruit", "🍎", 42),
        new("Harvest", "🌾", 52),
    };

    // The number of ministry weeks (Sundays) a start→end window spans, clamped to ≥ 1.
    public static int WeeksBetween(DateTime start, DateTime end) =>
        Math.Max(1, (int)Math.Round((end - start).TotalDays / 7.0));

    // Growth points that mark Harvest for a season of the given length.
    public static int HarvestPointsFor(int weeks) => Math.Max(1, weeks) * WeeklyPoints;

    // The seven stage thresholds scaled to a season of `weeks` weeks.
    public static GrowthStage[] StagesForWeeks(int weeks)
    {
        var w = Math.Max(1, weeks);
        return Curve.Select((d, i) => new GrowthStage(
            i, d.Name, d.Emoji,
            (int)Math.Round((double)d.Sundays * w / 52.0) * WeeklyPoints)).ToArray();
    }

    public static GrowthStage StageFor(int gp, GrowthStage[] stages)
    {
        var stage = stages[0];
        foreach (var s in stages)
            if (gp >= s.Gp)
                stage = s;
        return stage;
    }

    public static GrowthStage? NextStage(int index, GrowthStage[] stages) =>
        index + 1 < stages.Length ? stages[index + 1] : null;
}
