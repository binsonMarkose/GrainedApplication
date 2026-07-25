using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Progress;

public class ChildProgressService(IApplicationDbContext db) : IChildProgressService
{
    public async Task<List<ChildProgressDto>> GetForChildAsync(Guid childId, Guid churchId, CancellationToken ct = default)
    {
        var childExists = await db.Children.AsNoTracking().AnyAsync(c => c.Id == childId && c.ChurchId == churchId, ct);
        if (!childExists)
            throw new ValidationException("Child not found.");

        return await db.ChildProgresses.AsNoTracking()
            .Include(p => p.Lesson)
            .Where(p => p.ChildId == childId)
            .OrderByDescending(p => p.CompletedAtUtc)
            .Select(p => new ChildProgressDto(p.ChildId, p.LessonId, p.Lesson.Title, p.CompletedAtUtc, p.QuizScore,
                p.MemoryVerseCompleted, p.ActivityCompleted, p.PrayerCompleted))
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(Guid churchId, ChildProgressUpdateModel model, CancellationToken ct = default)
    {
        var childExists = await db.Children.AsNoTracking().AnyAsync(c => c.Id == model.ChildId && c.ChurchId == churchId, ct);
        if (!childExists)
            throw new ValidationException("Child not found.");

        var lessonExists = await db.Lessons.AsNoTracking().AnyAsync(l => l.Id == model.LessonId && l.ChurchId == churchId, ct);
        if (!lessonExists)
            throw new ValidationException("Lesson not found.");

        var progress = await db.ChildProgresses
            .FirstOrDefaultAsync(p => p.ChildId == model.ChildId && p.LessonId == model.LessonId, ct);

        if (progress is null)
        {
            progress = new Domain.Entities.ChildProgress { ChildId = model.ChildId, LessonId = model.LessonId };
            db.ChildProgresses.Add(progress);
        }

        progress.QuizScore = model.QuizScore;
        progress.MemoryVerseCompleted = model.MemoryVerseCompleted;
        progress.ActivityCompleted = model.ActivityCompleted;
        progress.PrayerCompleted = model.PrayerCompleted;

        var allDone = model.MemoryVerseCompleted && model.ActivityCompleted && model.PrayerCompleted;
        progress.CompletedAtUtc = allDone ? progress.CompletedAtUtc ?? DateTime.UtcNow : null;

        await db.SaveChangesAsync(ct);
    }
}
