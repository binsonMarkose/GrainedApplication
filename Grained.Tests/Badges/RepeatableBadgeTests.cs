using Grained.Application.Badges;
using Grained.Application.Common.Exceptions;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Grained.Infrastructure.Persistence;
using Grained.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace Grained.Tests.Badges;

public class RepeatableBadgeTests
{
    private static async Task<(ApplicationDbContext db, Church church, Child child)> SetupAsync()
    {
        var db = TestDbContextFactory.Create();
        var church = new Church { Name = "Peniel", Email = "p@x.org" };
        db.Churches.Add(church);
        var group = new ClassGroup { Church = church, Name = "Nursery", MinAge = 3, MaxAge = 5 };
        db.ClassGroups.Add(group);
        var child = new Child
        {
            ChurchId = church.Id,
            ClassGroupId = group.Id,
            FirstName = "Ada",
            LastName = "L",
            ParentName = "P",
            ParentEmail = "p@example.com",
        };
        db.Children.Add(child);
        await db.SaveChangesAsync();
        return (db, church, child);
    }

    private static async Task<Guid> AddBadgeAsync(ApplicationDbContext db, Guid churchId, bool repeatable, BadgeTier tier)
    {
        var badge = new Badge { ChurchId = churchId, Name = repeatable ? "Kind Heart" : "Baptised", Tier = tier, Points = 12, Repeatable = repeatable };
        db.Badges.Add(badge);
        await db.SaveChangesAsync();
        return badge.Id;
    }

    [Fact]
    public async Task Repeatable_badge_can_be_awarded_multiple_times()
    {
        var (db, church, child) = await SetupAsync();
        var badgeId = await AddBadgeAsync(db, church.Id, repeatable: true, BadgeTier.Standard);
        var service = new BadgeService(db);

        await service.AwardToChildAsync(church.Id, child.Id, badgeId);
        await service.AwardToChildAsync(church.Id, child.Id, badgeId);
        await service.AwardToChildAsync(church.Id, child.Id, badgeId);

        Assert.Equal(3, await db.ChildBadges.CountAsync(cb => cb.ChildId == child.Id && cb.BadgeId == badgeId));
    }

    [Fact]
    public async Task One_time_badge_cannot_be_awarded_twice()
    {
        var (db, church, child) = await SetupAsync();
        var badgeId = await AddBadgeAsync(db, church.Id, repeatable: false, BadgeTier.Achievement);
        var service = new BadgeService(db);

        await service.AwardToChildAsync(church.Id, child.Id, badgeId);
        await Assert.ThrowsAsync<ValidationException>(() => service.AwardToChildAsync(church.Id, child.Id, badgeId));

        Assert.Equal(1, await db.ChildBadges.CountAsync(cb => cb.ChildId == child.Id && cb.BadgeId == badgeId));
    }

    [Fact]
    public async Task Default_badges_seed_standard_as_repeatable_and_achievements_as_one_time()
    {
        var db = TestDbContextFactory.Create();
        foreach (var b in DefaultBadges.ForChurch(Guid.NewGuid()))
        {
            if (b.Tier == BadgeTier.Standard) Assert.True(b.Repeatable, $"{b.Name} should be repeatable");
            else Assert.False(b.Repeatable, $"{b.Name} should be one-time");
        }
    }
}
