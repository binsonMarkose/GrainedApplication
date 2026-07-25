using Grained.Application.Announcements;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Grained.Tests.Common;

namespace Grained.Tests.Announcements;

public class AnnouncementServiceTests
{
    private static async Task<(AnnouncementService service, Guid churchId)> SeedAsync()
    {
        var db = TestDbContextFactory.Create();
        var church = new Church { Name = "Grace", Email = "grace@church.org" };
        db.Churches.Add(church);
        await db.SaveChangesAsync();

        var service = new AnnouncementService(db);
        var author = Guid.NewGuid();
        await service.CreateAsync(church.Id, author, "Pastor", new AnnouncementFormModel { Title = "For teachers", Body = "b", Audience = AnnouncementAudience.Teachers });
        await service.CreateAsync(church.Id, author, "Pastor", new AnnouncementFormModel { Title = "For parents", Body = "b", Audience = AnnouncementAudience.Parents });
        await service.CreateAsync(church.Id, author, "Pastor", new AnnouncementFormModel { Title = "For all", Body = "b", Audience = AnnouncementAudience.Everyone });
        return (service, church.Id);
    }

    [Fact]
    public async Task Teacher_Inbox_HasTeacherAndEveryone_NotParents()
    {
        var (service, churchId) = await SeedAsync();

        var inbox = await service.GetInboxAsync(churchId, Guid.NewGuid(), isTeacher: true, isParent: false);

        Assert.Equal(2, inbox.Count);
        Assert.Contains(inbox, a => a.Title == "For teachers");
        Assert.Contains(inbox, a => a.Title == "For all");
        Assert.DoesNotContain(inbox, a => a.Title == "For parents");
    }

    [Fact]
    public async Task Parent_Inbox_HasParentAndEveryone_NotTeachers()
    {
        var (service, churchId) = await SeedAsync();

        var inbox = await service.GetInboxAsync(churchId, Guid.NewGuid(), isTeacher: false, isParent: true);

        Assert.Equal(2, inbox.Count);
        Assert.Contains(inbox, a => a.Title == "For parents");
        Assert.Contains(inbox, a => a.Title == "For all");
        Assert.DoesNotContain(inbox, a => a.Title == "For teachers");
    }

    [Fact]
    public async Task PureAdmin_Receives_Nothing()
    {
        var (service, churchId) = await SeedAsync();

        var inbox = await service.GetInboxAsync(churchId, Guid.NewGuid(), isTeacher: false, isParent: false);

        Assert.Empty(inbox);
    }

    [Fact]
    public async Task MarkRead_MakesItemRead_AndCountsForAuthor()
    {
        var (service, churchId) = await SeedAsync();
        var userId = Guid.NewGuid();

        var before = await service.GetInboxAsync(churchId, userId, isTeacher: true, isParent: false);
        var everyone = before.Single(a => a.Title == "For all");
        Assert.False(everyone.IsRead);

        await service.MarkReadAsync(everyone.Id, churchId, userId);

        var after = await service.GetInboxAsync(churchId, userId, isTeacher: true, isParent: false);
        Assert.True(after.Single(a => a.Title == "For all").IsRead);

        // The author's list shows one reader for that announcement.
        var forAuthor = await service.GetForChurchAsync(churchId);
        Assert.Equal(1, forAuthor.Single(a => a.Title == "For all").ReadCount);
    }

    [Fact]
    public async Task Retracted_Announcement_DropsOutOfInbox()
    {
        var (service, churchId) = await SeedAsync();
        var userId = Guid.NewGuid();

        var inbox = await service.GetInboxAsync(churchId, userId, isTeacher: true, isParent: false);
        var teachers = inbox.Single(a => a.Title == "For teachers");

        await service.SetActiveAsync(teachers.Id, churchId, isActive: false);

        var after = await service.GetInboxAsync(churchId, userId, isTeacher: true, isParent: false);
        Assert.DoesNotContain(after, a => a.Title == "For teachers");
    }

    [Fact]
    public async Task MarkAllRead_ClearsEverythingDeliverable()
    {
        var (service, churchId) = await SeedAsync();
        var userId = Guid.NewGuid();

        await service.MarkAllReadAsync(churchId, userId, isTeacher: true, isParent: false);

        var after = await service.GetInboxAsync(churchId, userId, isTeacher: true, isParent: false);
        Assert.All(after, a => Assert.True(a.IsRead));
    }
}
