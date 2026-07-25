using Grained.Application.Common.Models;
using Grained.Application.Progress;
using Grained.Domain.Entities;
using Grained.Tests.Common;

namespace Grained.Tests.Progress;

public class ChildProgressServiceTests
{
    private static async Task<(ChildProgressService Service, Guid ChurchId, Guid ChildId, Guid LessonId)> SeedAsync()
    {
        var db = TestDbContextFactory.Create();
        var church = new Church { Name = "Test Church", Email = "test@church.org" };
        var classGroup = new ClassGroup { Church = church, Name = "Ages 5-10", MinAge = 5, MaxAge = 10 };
        var child = new Child
        {
            Church = church,
            ClassGroup = classGroup,
            FirstName = "Timmy",
            LastName = "Tester",
            DateOfBirth = new DateOnly(2018, 1, 1),
            ParentName = "Parent",
            ParentEmail = "parent@example.com"
        };
        var lesson = new Lesson
        {
            Church = church,
            Title = "Creation",
            BibleReference = "Genesis 1",
            AgeGroup = "Ages 5-10",
            StoryContent = "..."
        };
        db.Churches.Add(church);
        db.ClassGroups.Add(classGroup);
        db.Children.Add(child);
        db.Lessons.Add(lesson);
        await db.SaveChangesAsync();

        return (new ChildProgressService(db), church.Id, child.Id, lesson.Id);
    }

    [Fact]
    public async Task UpsertAsync_CreatesNewProgressRecord()
    {
        var (service, churchId, childId, lessonId) = await SeedAsync();

        await service.UpsertAsync(churchId, new ChildProgressUpdateModel
        {
            ChildId = childId,
            LessonId = lessonId,
            QuizScore = 80,
            MemoryVerseCompleted = true,
            ActivityCompleted = false,
            PrayerCompleted = false
        });

        var progress = Assert.Single(await service.GetForChildAsync(childId, churchId));
        Assert.Equal(80, progress.QuizScore);
        Assert.True(progress.MemoryVerseCompleted);
        Assert.Null(progress.CompletedAtUtc);
    }

    [Fact]
    public async Task UpsertAsync_SetsCompletedAtUtc_WhenAllThreeFlagsAreTrue()
    {
        var (service, churchId, childId, lessonId) = await SeedAsync();

        await service.UpsertAsync(churchId, new ChildProgressUpdateModel
        {
            ChildId = childId,
            LessonId = lessonId,
            MemoryVerseCompleted = true,
            ActivityCompleted = true,
            PrayerCompleted = true
        });

        var progress = Assert.Single(await service.GetForChildAsync(childId, churchId));
        Assert.NotNull(progress.CompletedAtUtc);
    }

    [Fact]
    public async Task UpsertAsync_CalledTwice_UpdatesSameRecordRatherThanDuplicating()
    {
        var (service, churchId, childId, lessonId) = await SeedAsync();

        await service.UpsertAsync(churchId, new ChildProgressUpdateModel { ChildId = childId, LessonId = lessonId, QuizScore = 50 });
        await service.UpsertAsync(churchId, new ChildProgressUpdateModel { ChildId = childId, LessonId = lessonId, QuizScore = 90 });

        var progress = Assert.Single(await service.GetForChildAsync(childId, churchId));
        Assert.Equal(90, progress.QuizScore);
    }
}
