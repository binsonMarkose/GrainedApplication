using Grained.Application.Badges;
using Grained.Application.Onboarding;
using Grained.Domain.Enums;
using Grained.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace Grained.Tests.Onboarding;

public class DefaultBadgesTests
{
    // Minimal token service — the badge seeding doesn't depend on real tokens.
    private sealed class FakeInviteTokenService : IInviteTokenService
    {
        public string CreateToken(Guid invitationId, TimeSpan lifetime) => invitationId.ToString();
        public bool TryValidate(string token, out Guid invitationId) => Guid.TryParse(token, out invitationId);
        public string Hash(string token) => token;
    }

    [Fact]
    public async Task Provisioning_a_church_seeds_the_ten_default_badges()
    {
        var db = TestDbContextFactory.Create();
        var service = new ChurchOnboardingService(db, new FakeInviteTokenService());

        var created = await service.CreateChurchWithInviteAsync("New Life Church", "admin@newlife.org", Guid.NewGuid());

        var badges = await db.Badges.Where(b => b.ChurchId == created.ChurchId).ToListAsync();
        Assert.Equal(10, badges.Count);
        Assert.Equal(DefaultBadges.Catalog.Count, badges.Count);
        // Every badge has an icon + name, and the set includes at least one Achievement-tier milestone.
        Assert.All(badges, b => Assert.False(string.IsNullOrWhiteSpace(b.IconName)));
        Assert.All(badges, b => Assert.False(string.IsNullOrWhiteSpace(b.Name)));
        Assert.Contains(badges, b => b.Tier == BadgeTier.Achievement);
        Assert.All(badges, b => Assert.True(b.Points > 0));
    }

    [Fact]
    public async Task Default_badges_are_scoped_to_their_own_church()
    {
        var db = TestDbContextFactory.Create();
        var service = new ChurchOnboardingService(db, new FakeInviteTokenService());

        var a = await service.CreateChurchWithInviteAsync("Church A", "a@a.org", Guid.NewGuid());
        var b = await service.CreateChurchWithInviteAsync("Church B", "b@b.org", Guid.NewGuid());

        Assert.Equal(10, await db.Badges.CountAsync(x => x.ChurchId == a.ChurchId));
        Assert.Equal(10, await db.Badges.CountAsync(x => x.ChurchId == b.ChurchId));
        Assert.Equal(20, await db.Badges.CountAsync());
    }
}
