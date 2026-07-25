using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Growth;

public class GrowthService(IApplicationDbContext db) : IGrowthService
{
    // A season's contribution window — the admin-defined ministry year [Start, End]. Activity
    // (lessons, attendance, badges) whose timestamp falls inside counts toward that season's tree.
    private record Window(string Name, DateTime Start, DateTime End);

    // Guard against a legacy/blank end so a window is always a sensible span.
    private static DateTime EffectiveEnd(DateTime start, DateTime end) => end > start ? end : start.AddYears(1);

    private static Window ToWindow(GrowthSeason s) => new(s.Name, s.StartsOnUtc, EffectiveEnd(s.StartsOnUtc, s.EndsOnUtc));

    // The implicit season for a church that hasn't set one up yet: the current calendar year.
    private static Window DefaultWindow(DateTime now)
    {
        var start = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        return new Window(now.Year.ToString(), start, start.AddYears(1));
    }

    private static List<Window> BuildWindows(List<GrowthSeason> seasons, DateTime now) =>
        seasons.Count == 0 ? [DefaultWindow(now)] : seasons.Select(ToWindow).ToList();

    private async Task<List<GrowthSeason>> GetSeasonsAsync(Guid churchId, CancellationToken ct) =>
        await db.GrowthSeasons.AsNoTracking()
            .Where(s => s.ChurchId == churchId)
            .OrderBy(s => s.StartsOnUtc)
            .ToListAsync(ct);

    private static GrowthSeasonDto ToDto(GrowthSeason s, bool isCurrent)
    {
        var end = EffectiveEnd(s.StartsOnUtc, s.EndsOnUtc);
        var weeks = GrowthLevels.WeeksBetween(s.StartsOnUtc, end);
        return new GrowthSeasonDto(s.Id, s.Name, s.StartsOnUtc, end, weeks, GrowthLevels.HarvestPointsFor(weeks), isCurrent);
    }

    public async Task<List<GrowthSeasonDto>> ListSeasonsAsync(Guid churchId, CancellationToken ct = default)
    {
        var seasons = await GetSeasonsAsync(churchId, ct);
        if (seasons.Count == 0)
        {
            // Nothing set up yet — present the implicit current-year season (52 weeks).
            var w = DefaultWindow(DateTime.UtcNow);
            return [new GrowthSeasonDto(Guid.Empty, w.Name, w.Start, w.End, 52, GrowthLevels.HarvestPointsFor(52), true)];
        }
        return seasons.Select((s, i) => ToDto(s, i == seasons.Count - 1)).ToList();
    }

    private static (DateTime start, DateTime end, string name) Validate(GrowthSeasonFormModel model)
    {
        var start = DateTime.SpecifyKind(model.StartsOnUtc, DateTimeKind.Utc);
        var end = DateTime.SpecifyKind(model.EndsOnUtc, DateTimeKind.Utc);
        if (end <= start)
            throw new ValidationException("The season's end date must be after its start date.");
        var name = string.IsNullOrWhiteSpace(model.Name) ? start.Year.ToString() : model.Name.Trim();
        return (start, end, name);
    }

    public async Task<GrowthSeasonDto> CreateSeasonAsync(Guid churchId, GrowthSeasonFormModel model, CancellationToken ct = default)
    {
        var (start, end, name) = Validate(model);

        var season = new GrowthSeason { ChurchId = churchId, Name = name, StartsOnUtc = start, EndsOnUtc = end };
        db.GrowthSeasons.Add(season);
        await db.SaveChangesAsync(ct);

        var isCurrent = !await db.GrowthSeasons.AsNoTracking()
            .AnyAsync(s => s.ChurchId == churchId && s.Id != season.Id && s.StartsOnUtc > start, ct);
        return ToDto(season, isCurrent);
    }

    public async Task<GrowthSeasonDto> UpdateSeasonAsync(Guid id, Guid churchId, GrowthSeasonFormModel model, CancellationToken ct = default)
    {
        var (start, end, name) = Validate(model);

        var season = await db.GrowthSeasons.FirstOrDefaultAsync(s => s.Id == id && s.ChurchId == churchId, ct)
            ?? throw new ValidationException("Season not found.");

        season.Name = name;
        season.StartsOnUtc = start;
        season.EndsOnUtc = end;
        await db.SaveChangesAsync(ct);

        var isCurrent = !await db.GrowthSeasons.AsNoTracking()
            .AnyAsync(s => s.ChurchId == churchId && s.Id != season.Id && s.StartsOnUtc > start, ct);
        return ToDto(season, isCurrent);
    }

    public async Task<Dictionary<Guid, GrowthSummaryDto>> GetGrowthForChildrenAsync(
        IReadOnlyList<Guid> childIds, Guid churchId, CancellationToken ct = default)
    {
        var ids = childIds.Distinct().ToList();
        var result = new Dictionary<Guid, GrowthSummaryDto>();
        if (ids.Count == 0)
            return result;

        var now = DateTime.UtcNow;
        var windows = BuildWindows(await GetSeasonsAsync(churchId, ct), now);

        var completions = await db.ChildProgresses.AsNoTracking()
            .Where(p => ids.Contains(p.ChildId) && p.CompletedAtUtc != null)
            .Select(p => new { p.ChildId, When = p.CompletedAtUtc!.Value, p.MemoryVerseCompleted })
            .ToListAsync(ct);

        var attendance = await db.Attendances.AsNoTracking()
            .Where(a => ids.Contains(a.ChildId) && a.IsPresent)
            .Select(a => new { a.ChildId, a.AttendanceDate })
            .ToListAsync(ct);

        var badges = await db.ChildBadges.AsNoTracking()
            .Where(cb => ids.Contains(cb.ChildId))
            .Select(cb => new { cb.ChildId, cb.AwardedAtUtc, cb.Badge.Points, cb.Badge.Tier })
            .ToListAsync(ct);

        var created = await db.Children.AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.CreatedAtUtc })
            .ToDictionaryAsync(x => x.Id, x => x.CreatedAtUtc, ct);

        foreach (var cid in ids)
        {
            var forest = new List<GrowthForestEntryDto>();
            GrowthSummaryDto? current = null;

            for (var i = 0; i < windows.Count; i++)
            {
                var w = windows[i];
                var isCurrent = i == windows.Count - 1;
                var startDate = DateOnly.FromDateTime(w.Start);
                var endDate = DateOnly.FromDateTime(w.End);

                var windowCompletions = completions.Where(x => x.ChildId == cid && InWindow(x.When, w)).ToList();
                var lessons = windowCompletions.Count;
                var verses = windowCompletions.Count(x => x.MemoryVerseCompleted);
                var present = attendance.Count(x => x.ChildId == cid && x.AttendanceDate >= startDate && x.AttendanceDate <= endDate);
                var childBadges = badges.Where(x => x.ChildId == cid && InWindow(x.AwardedAtUtc, w)).ToList();

                var gp = lessons * GrowthLevels.LessonPoints
                         + present * GrowthLevels.AttendancePoints
                         + verses * GrowthLevels.VersePoints
                         + childBadges.Sum(b => b.Points);

                // Stage targets scale to this season's length so Harvest lands at the season's end.
                var stages = GrowthLevels.StagesForWeeks(GrowthLevels.WeeksBetween(w.Start, w.End));
                var stage = GrowthLevels.StageFor(gp, stages);

                if (isCurrent)
                {
                    var next = GrowthLevels.NextStage(stage.Index, stages);
                    current = new GrowthSummaryDto(
                        w.Name, stage.Index, stage.Name, stage.Emoji, gp, stage.Gp, next?.Gp, next?.Name,
                        lessons, present, verses,
                        childBadges.Count(b => b.Tier == BadgeTier.Standard),
                        childBadges.Count(b => b.Tier == BadgeTier.Achievement),
                        forest);
                }
                else
                {
                    var existed = !created.TryGetValue(cid, out var cAt) || cAt <= w.End;
                    if (existed)
                        forest.Add(new GrowthForestEntryDto(w.Name, stage.Index, stage.Name, stage.Emoji, gp));
                }
            }

            if (current is not null)
                result[cid] = current;
        }

        return result;
    }

    public async Task<List<ChildStageDto>> GetChildStagesAsync(Guid churchId, CancellationToken ct = default)
    {
        var childIds = await db.Children.AsNoTracking()
            .Where(c => c.ChurchId == churchId && c.IsActive)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var growth = await GetGrowthForChildrenAsync(childIds, churchId, ct);
        return growth
            .Select(kv => new ChildStageDto(kv.Key, kv.Value.StageIndex, kv.Value.StageName, kv.Value.StageEmoji, kv.Value.GrowthPoints))
            .ToList();
    }

    private static bool InWindow(DateTime when, Window w) => when >= w.Start && when <= w.End;
}
