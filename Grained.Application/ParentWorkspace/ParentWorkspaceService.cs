using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Application.Growth;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.ParentWorkspace;

public class ParentWorkspaceService(IApplicationDbContext db, IGrowthService growth) : IParentWorkspaceService
{
    public async Task<ParentWorkspaceDto> GetForParentAsync(Guid userId, Guid churchId, CancellationToken ct = default)
    {
        var parent = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        var parentName = parent?.FullName ?? string.Empty;

        var children = await db.Children.AsNoTracking()
            .Include(c => c.ClassGroup)
            .Where(c => c.ParentUserId == userId && c.ChurchId == churchId && c.IsActive)
            .OrderBy(c => c.FirstName).ThenBy(c => c.LastName)
            .ToListAsync(ct);

        if (children.Count == 0)
            return new ParentWorkspaceDto(parentName, []);

        var classGroupIds = children.Select(c => c.ClassGroupId).Distinct().ToList();
        var childIds = children.Select(c => c.Id).ToList();

        // The published lesson curriculum for the relevant classes, with memory verse.
        var curriculum = await db.LessonClassGroups.AsNoTracking()
            .Where(lcg => classGroupIds.Contains(lcg.ClassGroupId) && lcg.Lesson.IsPublished)
            .Select(lcg => new
            {
                lcg.ClassGroupId,
                lcg.Lesson.Id,
                lcg.Lesson.Title,
                lcg.Lesson.BibleReference,
                lcg.Lesson.Theme,
                MemoryVerseReference = lcg.Lesson.MemoryVerse != null ? lcg.Lesson.MemoryVerse.BibleReference : null,
                MemoryVerseText = lcg.Lesson.MemoryVerse != null ? lcg.Lesson.MemoryVerse.VerseText : null,
                lcg.SortOrder
            })
            .ToListAsync(ct);

        // A lesson counts as "taught" to a class once any child in that class has completed it.
        var taught = await db.ChildProgresses.AsNoTracking()
            .Where(p => p.CompletedAtUtc != null && classGroupIds.Contains(p.Child.ClassGroupId))
            .Select(p => new { p.Child.ClassGroupId, p.LessonId })
            .Distinct()
            .ToListAsync(ct);
        var taughtSet = taught.Select(t => (t.ClassGroupId, t.LessonId)).ToHashSet();

        // This parent's children's own completions.
        var myCompletions = await db.ChildProgresses.AsNoTracking()
            .Where(p => childIds.Contains(p.ChildId) && p.CompletedAtUtc != null)
            .Select(p => new { p.ChildId, p.LessonId, p.CompletedAtUtc })
            .ToListAsync(ct);
        var completedAt = myCompletions.ToDictionary(x => (x.ChildId, x.LessonId), x => x.CompletedAtUtc);

        // Badges earned.
        var badges = await db.ChildBadges.AsNoTracking()
            .Where(cb => childIds.Contains(cb.ChildId))
            .Select(cb => new { cb.ChildId, cb.BadgeId, cb.Badge.Name, cb.Badge.Description, cb.Badge.IconName, cb.AwardedAtUtc })
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var growthById = await growth.GetGrowthForChildrenAsync(childIds, churchId, ct);

        var childDtos = children.Select(c =>
        {
            var lessons = curriculum
                .Where(l => l.ClassGroupId == c.ClassGroupId)
                .OrderBy(l => l.SortOrder).ThenBy(l => l.Title) // teaching order
                .Select(l =>
                {
                    var isCompleted = completedAt.TryGetValue((c.Id, l.Id), out var when);
                    var isTaught = taughtSet.Contains((c.ClassGroupId, l.Id));
                    var status = isCompleted ? "Completed" : isTaught ? "Missed" : "Upcoming";
                    return new ParentLessonDto(
                        l.Id, l.Title, l.BibleReference, l.Theme, l.MemoryVerseReference, l.MemoryVerseText,
                        status, isCompleted ? when : null);
                })
                // Stable sort keeps teaching order within each status bucket.
                .OrderBy(l => l.Status == "Completed" ? 0 : l.Status == "Missed" ? 1 : 2)
                .ToList();

            var childBadges = badges
                .Where(b => b.ChildId == c.Id)
                .GroupBy(b => new { b.BadgeId, b.Name, b.Description, b.IconName })
                .Select(g => new ParentBadgeDto(
                    g.Key.BadgeId, g.Key.Name, g.Key.Description, g.Key.IconName, g.Max(x => x.AwardedAtUtc), g.Count()))
                .OrderByDescending(b => b.AwardedAtUtc)
                .ToList();

            return new ParentChildDto(
                c.Id, c.FirstName, c.LastName, AgeAt(c.DateOfBirth, today), c.ClassGroup.Name, c.AvatarId,
                growthById[c.Id],
                lessons.Count(l => l.Status == "Completed"),
                lessons.Count(l => l.Status == "Missed"),
                lessons.Count(l => l.Status == "Upcoming"),
                lessons.Count,
                lessons,
                childBadges);
        }).ToList();

        return new ParentWorkspaceDto(parentName, childDtos);
    }

    public async Task SetChildAvatarAsync(Guid userId, Guid churchId, Guid childId, string? avatarId, CancellationToken ct = default)
    {
        var child = await db.Children
            .FirstOrDefaultAsync(c => c.Id == childId && c.ParentUserId == userId && c.ChurchId == churchId, ct)
            ?? throw new ValidationException("Child not found.");

        child.AvatarId = string.IsNullOrWhiteSpace(avatarId) ? null : avatarId.Trim();
        await db.SaveChangesAsync(ct);
    }

    public async Task<ParentLessonDetailDto> GetLessonAsync(Guid userId, Guid churchId, Guid lessonId, CancellationToken ct = default)
    {
        // Only lessons published and assigned to a class one of this parent's children is in.
        var myClassGroupIds = await db.Children.AsNoTracking()
            .Where(c => c.ParentUserId == userId && c.ChurchId == churchId && c.IsActive)
            .Select(c => c.ClassGroupId)
            .Distinct()
            .ToListAsync(ct);

        var allowed = await db.LessonClassGroups.AsNoTracking()
            .AnyAsync(lcg => lcg.LessonId == lessonId && myClassGroupIds.Contains(lcg.ClassGroupId)
                             && lcg.Lesson.ChurchId == churchId && lcg.Lesson.IsPublished, ct);
        if (!allowed)
            throw new ValidationException("Lesson not found.");

        var lesson = await db.Lessons.AsNoTracking()
            .Include(l => l.MemoryVerse)
            .FirstOrDefaultAsync(l => l.Id == lessonId, ct)
            ?? throw new ValidationException("Lesson not found.");

        return new ParentLessonDetailDto(
            lesson.Id, lesson.Title, lesson.BibleReference, lesson.Theme, lesson.StoryContent,
            lesson.LearningObjective, lesson.Activity, lesson.Prayer,
            lesson.MemoryVerse?.BibleReference, lesson.MemoryVerse?.VerseText);
    }

    private static int AgeAt(DateOnly dob, DateOnly today)
    {
        var age = today.Year - dob.Year;
        if (dob > today.AddYears(-age))
            age--;
        return age;
    }
}
