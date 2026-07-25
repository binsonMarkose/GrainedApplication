using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Services;
using Grained.Application.Lessons;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Grained.Infrastructure.Persistence;
using Grained.Tests.Common;

namespace Grained.Tests.Lessons;

public class LessonOrderTests
{
    private static LessonService Service(ApplicationDbContext db) =>
        new(db, new TeacherScope(db, new FakeCurrentUserService()), new FakeCurrentUserService());

    private static Lesson NewLesson(Church church, string title) =>
        new() { ChurchId = church.Id, Title = title, BibleReference = "Gen 1" };

    private static Lesson PublishedLesson(Church church, string title) =>
        new() { ChurchId = church.Id, Title = title, BibleReference = "Gen 1", IsPublished = true, Status = LessonStatus.Published };

    [Fact]
    public async Task Assign_appends_to_end_and_reorder_sets_teaching_order()
    {
        var db = TestDbContextFactory.Create();
        var church = new Church { Name = "Peniel", Email = "p@x.org" };
        db.Churches.Add(church);
        var group = new ClassGroup { Church = church, Name = "Nursery", MinAge = 3, MaxAge = 5 };
        db.ClassGroups.Add(group);
        var one = NewLesson(church, "One");
        var two = NewLesson(church, "Two");
        var three = NewLesson(church, "Three");
        db.Lessons.AddRange(one, two, three);
        await db.SaveChangesAsync();

        var svc = Service(db);
        await svc.AssignToClassGroupAsync(one.Id, church.Id, group.Id);
        await svc.AssignToClassGroupAsync(two.Id, church.Id, group.Id);
        await svc.AssignToClassGroupAsync(three.Id, church.Id, group.Id);

        // Appended in assignment order.
        var initial = await svc.GetForChurchAsync(church.Id, classGroupId: group.Id);
        Assert.Equal(new[] { "One", "Two", "Three" }, initial.Select(l => l.Title));

        // Reorder → new teaching order.
        await svc.ReorderLessonsAsync(group.Id, church.Id, [three.Id, one.Id, two.Id]);
        var reordered = await svc.GetForChurchAsync(church.Id, classGroupId: group.Id);
        Assert.Equal(new[] { "Three", "One", "Two" }, reordered.Select(l => l.Title));
    }

    [Fact]
    public async Task Order_is_independent_per_group()
    {
        var db = TestDbContextFactory.Create();
        var church = new Church { Name = "Peniel", Email = "p@x.org" };
        db.Churches.Add(church);
        var a = new ClassGroup { Church = church, Name = "A", MinAge = 3, MaxAge = 5 };
        var b = new ClassGroup { Church = church, Name = "B", MinAge = 6, MaxAge = 8 };
        db.ClassGroups.AddRange(a, b);
        var one = NewLesson(church, "One");
        var two = NewLesson(church, "Two");
        db.Lessons.AddRange(one, two);
        await db.SaveChangesAsync();

        var svc = Service(db);
        foreach (var g in new[] { a.Id, b.Id })
        {
            await svc.AssignToClassGroupAsync(one.Id, church.Id, g);
            await svc.AssignToClassGroupAsync(two.Id, church.Id, g);
        }

        // Reorder only group A.
        await svc.ReorderLessonsAsync(a.Id, church.Id, [two.Id, one.Id]);

        var orderA = (await svc.GetForChurchAsync(church.Id, classGroupId: a.Id)).Select(l => l.Title).ToArray();
        var orderB = (await svc.GetForChurchAsync(church.Id, classGroupId: b.Id)).Select(l => l.Title).ToArray();
        Assert.Equal(new[] { "Two", "One" }, orderA);
        Assert.Equal(new[] { "One", "Two" }, orderB); // B is unchanged
    }

    [Fact]
    public async Task Teacher_can_reorder_own_group_but_not_others()
    {
        var db = TestDbContextFactory.Create();
        var church = new Church { Name = "Peniel", Email = "p@x.org" };
        var a = new ClassGroup { Church = church, Name = "A", MinAge = 3, MaxAge = 5 };
        var b = new ClassGroup { Church = church, Name = "B", MinAge = 6, MaxAge = 8 };
        var teacher = new ApplicationUser { UserName = "t@x.org", Email = "t@x.org", FullName = "T", Church = church };
        var profile = new TeacherProfile { ApplicationUser = teacher, Church = church };
        var assignment = new TeacherClassGroup { TeacherProfile = profile, ClassGroup = a };
        var one = PublishedLesson(church, "One");
        var two = PublishedLesson(church, "Two");
        db.AddRange(church, a, b, teacher, profile, assignment, one, two);
        db.LessonClassGroups.AddRange(
            new LessonClassGroup { Lesson = one, ClassGroup = a, SortOrder = 0 },
            new LessonClassGroup { Lesson = two, ClassGroup = a, SortOrder = 1 },
            new LessonClassGroup { Lesson = one, ClassGroup = b, SortOrder = 0 },
            new LessonClassGroup { Lesson = two, ClassGroup = b, SortOrder = 1 });
        await db.SaveChangesAsync();

        var currentUser = new FakeCurrentUserService { UserId = teacher.Id, ChurchId = church.Id, IsChurchAdmin = false, IsTeacher = true };
        var svc = new LessonService(db, new TeacherScope(db, currentUser), currentUser);

        // Their own group A → allowed.
        await svc.ReorderLessonsAsync(a.Id, church.Id, [two.Id, one.Id]);
        var orderA = (await svc.GetForChurchAsync(church.Id, classGroupId: a.Id)).Select(l => l.Title).ToArray();
        Assert.Equal(new[] { "Two", "One" }, orderA);

        // A group they aren't assigned to → blocked.
        await Assert.ThrowsAsync<ValidationException>(() => svc.ReorderLessonsAsync(b.Id, church.Id, [two.Id, one.Id]));
    }
}
