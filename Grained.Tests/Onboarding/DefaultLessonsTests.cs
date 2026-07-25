using Grained.Application.Common.Services;
using Grained.Application.Lessons;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Grained.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace Grained.Tests.Onboarding;

public class DefaultLessonsTests
{
    private static LessonService Lessons(Grained.Infrastructure.Persistence.ApplicationDbContext db) =>
        new(db, new TeacherScope(db, new FakeCurrentUserService()), new FakeCurrentUserService());

    private static Church SeedChurch(Grained.Infrastructure.Persistence.ApplicationDbContext db)
    {
        var church = new Church { Name = "New Life", Email = "n@l.org" };
        db.Churches.Add(church);
        db.SaveChanges();
        return church;
    }

    [Fact]
    public async Task Seeding_adds_the_full_publish_ready_library()
    {
        var db = TestDbContextFactory.Create();
        var church = SeedChurch(db);

        var added = await Lessons(db).SeedDefaultLibraryAsync(church.Id);

        Assert.Equal(DefaultLessons.Catalog.Count, added);
        Assert.Equal(20, added);

        var lessons = await db.Lessons
            .Include(l => l.MemoryVerse)
            .Include(l => l.Quiz).ThenInclude(q => q!.Questions).ThenInclude(q => q.Options)
            .Where(l => l.ChurchId == church.Id)
            .ToListAsync();

        // Every seeded lesson is Published and meets the publish gates: a memory verse and at least
        // one quiz question that has a correct answer.
        Assert.All(lessons, l =>
        {
            Assert.Equal(LessonStatus.Published, l.Status);
            Assert.True(l.IsPublished);
            Assert.NotNull(l.MemoryVerse);
            Assert.False(string.IsNullOrWhiteSpace(l.MemoryVerse!.VerseText));
            Assert.NotNull(l.Quiz);
            Assert.NotEmpty(l.Quiz!.Questions);
            Assert.All(l.Quiz.Questions, q => Assert.Contains(q.Options, o => o.IsCorrect));
        });
    }

    [Fact]
    public async Task Seeding_is_idempotent()
    {
        var db = TestDbContextFactory.Create();
        var church = SeedChurch(db);
        var service = Lessons(db);

        var first = await service.SeedDefaultLibraryAsync(church.Id);
        var second = await service.SeedDefaultLibraryAsync(church.Id);

        Assert.Equal(20, first);
        Assert.Equal(0, second);
        Assert.Equal(20, await db.Lessons.CountAsync(l => l.ChurchId == church.Id));
    }
}
