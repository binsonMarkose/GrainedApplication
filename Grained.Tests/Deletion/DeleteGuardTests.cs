using Grained.Application.ClassGroups;
using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Services;
using Grained.Application.Events;
using Grained.Application.Fundraising;
using Grained.Application.Lessons;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Grained.Infrastructure.Persistence;
using Grained.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace Grained.Tests.Deletion;

// ChurchAdmin delete actions: a hard delete is allowed only when nothing important is linked;
// otherwise the service throws a friendly ValidationException and the admin disables instead.
public class DeleteGuardTests
{
    private static (ApplicationDbContext db, Church church) Seed()
    {
        var db = TestDbContextFactory.Create();
        var church = new Church { Name = "Grace", Email = "grace@church.org" };
        db.Churches.Add(church);
        db.SaveChanges();
        return (db, church);
    }

    private static ClassGroupService ClassGroups(ApplicationDbContext db) =>
        new(db, new TeacherScope(db, new FakeCurrentUserService()));

    private static LessonService LessonSvc(ApplicationDbContext db) =>
        new(db, new TeacherScope(db, new FakeCurrentUserService()), new FakeCurrentUserService());

    private static (ClassGroup group, Child child) SeedClassAndChild(ApplicationDbContext db, Church church)
    {
        var group = new ClassGroup { ChurchId = church.Id, Name = "Ages 5-10", MinAge = 5, MaxAge = 10 };
        db.ClassGroups.Add(group);
        var child = new Child
        {
            ChurchId = church.Id,
            ClassGroupId = group.Id,
            FirstName = "Ada",
            LastName = "Lovelace",
            ParentName = "Parent",
            ParentEmail = "p@example.com",
        };
        db.Children.Add(child);
        db.SaveChanges();
        return (group, child);
    }

    [Fact]
    public async Task ClassGroup_delete_succeeds_when_empty()
    {
        var (db, church) = Seed();
        var service = ClassGroups(db);
        var id = await service.CreateAsync(church.Id, new ClassGroupFormModel { Name = "Toddlers", MinAge = 1, MaxAge = 3 });

        await service.DeleteAsync(id, church.Id);

        Assert.False(await db.ClassGroups.AnyAsync(c => c.Id == id));
    }

    [Fact]
    public async Task ClassGroup_delete_blocked_when_it_has_children()
    {
        var (db, church) = Seed();
        var service = ClassGroups(db);
        var id = await service.CreateAsync(church.Id, new ClassGroupFormModel { Name = "Toddlers", MinAge = 1, MaxAge = 3 });
        db.Children.Add(new Child
        {
            ChurchId = church.Id,
            ClassGroupId = id,
            FirstName = "Ada",
            LastName = "Lovelace",
            ParentName = "Parent",
            ParentEmail = "p@example.com",
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationException>(() => service.DeleteAsync(id, church.Id));
        Assert.True(await db.ClassGroups.AnyAsync(c => c.Id == id));
    }

    [Fact]
    public async Task ClassGroup_delete_rejects_other_churches_group()
    {
        var (db, church) = Seed();
        var other = new Church { Name = "Other", Email = "o@church.org" };
        db.Churches.Add(other);
        await db.SaveChangesAsync();
        var service = ClassGroups(db);
        var id = await service.CreateAsync(church.Id, new ClassGroupFormModel { Name = "Toddlers", MinAge = 1, MaxAge = 3 });

        await Assert.ThrowsAsync<ValidationException>(() => service.DeleteAsync(id, other.Id));
    }

    [Fact]
    public async Task Event_delete_blocked_when_it_has_registrations()
    {
        var (db, church) = Seed();
        var ev = new Event { ChurchId = church.Id, Title = "Camp", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow };
        db.Events.Add(ev);
        db.EventRegistrations.Add(new EventRegistration
        {
            EventId = ev.Id,
            PurchaserName = "Buyer",
            PurchaserEmail = "b@example.com",
            Total = 10m,
        });
        await db.SaveChangesAsync();
        var service = new EventService(db);

        await Assert.ThrowsAsync<ValidationException>(() => service.DeleteAsync(ev.Id, church.Id));
        Assert.True(await db.Events.AnyAsync(e => e.Id == ev.Id));
    }

    [Fact]
    public async Task Event_delete_succeeds_without_registrations()
    {
        var (db, church) = Seed();
        var ev = new Event { ChurchId = church.Id, Title = "Camp", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow };
        db.Events.Add(ev);
        await db.SaveChangesAsync();
        var service = new EventService(db);

        await service.DeleteAsync(ev.Id, church.Id);

        Assert.False(await db.Events.AnyAsync(e => e.Id == ev.Id));
    }

    [Fact]
    public async Task Lesson_delete_succeeds_when_unused()
    {
        var (db, church) = Seed();
        var lesson = new Lesson { ChurchId = church.Id, Title = "Noah", BibleReference = "Genesis 6", Status = LessonStatus.Draft };
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();

        await LessonSvc(db).DeleteAsync(lesson.Id, church.Id);

        Assert.False(await db.Lessons.AnyAsync(l => l.Id == lesson.Id));
    }

    [Fact]
    public async Task Lesson_delete_blocked_when_a_child_has_progress()
    {
        var (db, church) = Seed();
        var (_, child) = SeedClassAndChild(db, church);
        var lesson = new Lesson { ChurchId = church.Id, Title = "Noah", BibleReference = "Genesis 6", Status = LessonStatus.Published, IsPublished = true };
        db.Lessons.Add(lesson);
        db.ChildProgresses.Add(new ChildProgress { ChildId = child.Id, LessonId = lesson.Id, CompletedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationException>(() => LessonSvc(db).DeleteAsync(lesson.Id, church.Id));
        Assert.True(await db.Lessons.AnyAsync(l => l.Id == lesson.Id));
    }

    [Fact]
    public async Task Lesson_delete_blocked_when_attendance_recorded()
    {
        var (db, church) = Seed();
        var (group, child) = SeedClassAndChild(db, church);
        var lesson = new Lesson { ChurchId = church.Id, Title = "Noah", BibleReference = "Genesis 6", Status = LessonStatus.Published, IsPublished = true };
        db.Lessons.Add(lesson);
        db.Attendances.Add(new Attendance
        {
            ChildId = child.Id,
            ClassGroupId = group.Id,
            LessonId = lesson.Id,
            AttendanceDate = new DateOnly(2026, 1, 4),
            IsPresent = true,
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationException>(() => LessonSvc(db).DeleteAsync(lesson.Id, church.Id));
        Assert.True(await db.Lessons.AnyAsync(l => l.Id == lesson.Id));
    }

    [Fact]
    public async Task Campaign_delete_blocked_when_it_has_donations()
    {
        var (db, church) = Seed();
        var campaign = new Campaign { ChurchId = church.Id, Title = "Roof" };
        db.Campaigns.Add(campaign);
        db.Donations.Add(new Donation
        {
            CampaignId = campaign.Id,
            DonorName = "Giver",
            DonorEmail = "g@example.com",
            Amount = 25m,
        });
        await db.SaveChangesAsync();
        var service = new CampaignService(db);

        await Assert.ThrowsAsync<ValidationException>(() => service.DeleteAsync(campaign.Id, church.Id));
        Assert.True(await db.Campaigns.AnyAsync(c => c.Id == campaign.Id));
    }

    [Fact]
    public async Task Campaign_delete_succeeds_and_removes_logo_image()
    {
        var (db, church) = Seed();
        var image = new StoredImage { Data = [1, 2, 3], ContentType = "image/png" };
        db.StoredImages.Add(image);
        var campaign = new Campaign { ChurchId = church.Id, Title = "Roof", LogoImageId = image.Id };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();
        var service = new CampaignService(db);

        await service.DeleteAsync(campaign.Id, church.Id);

        Assert.False(await db.Campaigns.AnyAsync(c => c.Id == campaign.Id));
        Assert.False(await db.StoredImages.AnyAsync(i => i.Id == image.Id));
    }
}
