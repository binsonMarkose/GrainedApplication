using Grained.Application.Common.Exceptions;
using Grained.Application.Growth;
using Grained.Domain.Entities;
using Grained.Tests.Common;

namespace Grained.Tests.Growth;

public class GrowthScalingTests
{
    // ---- Pure threshold scaling ----

    [Fact]
    public void StagesForWeeks_52_ReproducesOriginalTargets()
    {
        var stages = GrowthLevels.StagesForWeeks(52);
        Assert.Equal(new[] { 0, 60, 144, 252, 372, 504, 624 }, stages.Select(s => s.Gp).ToArray());
        Assert.Equal("Harvest", stages[^1].Name);
    }

    [Fact]
    public void StagesForWeeks_ShorterYear_ScalesHarvestToYearEnd()
    {
        // A 40-week ministry year: Harvest = 40 faithful Sundays × 12 = 480.
        var stages = GrowthLevels.StagesForWeeks(40);
        Assert.Equal(480, stages[^1].Gp);
        Assert.Equal(0, stages[0].Gp);
        // Intermediate stages keep the same relative curve, strictly increasing.
        for (var i = 1; i < stages.Length; i++)
            Assert.True(stages[i].Gp > stages[i - 1].Gp);
    }

    [Fact]
    public void HarvestPointsFor_MatchesWeeksTimesWeekly()
    {
        Assert.Equal(52 * 12, GrowthLevels.HarvestPointsFor(52));
        Assert.Equal(43 * 12, GrowthLevels.HarvestPointsFor(43));
    }

    [Fact]
    public void WeeksBetween_SepToJune_IsAboutFortyThree()
    {
        var weeks = GrowthLevels.WeeksBetween(new DateTime(2026, 9, 1), new DateTime(2027, 6, 30));
        Assert.InRange(weeks, 42, 44);
    }

    // ---- Season CRUD ----

    private static async Task<(GrowthService svc, Guid churchId)> SeedAsync()
    {
        var db = TestDbContextFactory.Create();
        var church = new Church { Name = "Grace", Email = "g@c.org" };
        db.Churches.Add(church);
        await db.SaveChangesAsync();
        return (new GrowthService(db), church.Id);
    }

    [Fact]
    public async Task CreateSeason_ComputesWeeksAndHarvest()
    {
        var (svc, churchId) = await SeedAsync();

        var dto = await svc.CreateSeasonAsync(churchId,
            new GrowthSeasonFormModel("2026–27", new DateTime(2026, 9, 6), new DateTime(2027, 6, 27)));

        Assert.Equal("2026–27", dto.Name);
        Assert.True(dto.IsCurrent);
        Assert.InRange(dto.Weeks, 41, 43);
        Assert.Equal(dto.Weeks * 12, dto.HarvestPoints);
    }

    [Fact]
    public async Task CreateSeason_EndBeforeStart_Throws()
    {
        var (svc, churchId) = await SeedAsync();
        await Assert.ThrowsAsync<ValidationException>(() => svc.CreateSeasonAsync(churchId,
            new GrowthSeasonFormModel("Bad", new DateTime(2027, 1, 1), new DateTime(2026, 1, 1))));
    }

    [Fact]
    public async Task UpdateSeason_ChangesDatesAndRescales()
    {
        var (svc, churchId) = await SeedAsync();
        var created = await svc.CreateSeasonAsync(churchId,
            new GrowthSeasonFormModel(null, new DateTime(2026, 1, 1), new DateTime(2027, 1, 1)));
        Assert.Equal(52, created.Weeks);

        var updated = await svc.UpdateSeasonAsync(created.Id, churchId,
            new GrowthSeasonFormModel("Fall", new DateTime(2026, 9, 1), new DateTime(2026, 12, 24)));

        Assert.Equal("Fall", updated.Name);
        Assert.InRange(updated.Weeks, 15, 17); // ~16 weeks
        Assert.Equal(updated.Weeks * 12, updated.HarvestPoints);
    }

    [Fact]
    public async Task ListSeasons_NoSeasons_ReturnsDefaultCalendarYear()
    {
        var (svc, churchId) = await SeedAsync();
        var list = await svc.ListSeasonsAsync(churchId);
        var only = Assert.Single(list);
        Assert.Equal(52, only.Weeks);
        Assert.True(only.IsCurrent);
    }
}
