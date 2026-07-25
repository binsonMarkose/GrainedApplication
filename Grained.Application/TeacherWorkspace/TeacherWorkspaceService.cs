using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.TeacherWorkspace;

public class TeacherWorkspaceService(IApplicationDbContext db, ITeacherScope teacherScope) : ITeacherWorkspaceService
{
    public async Task<TeacherWorkspaceDto> GetWorkspaceAsync(Guid userId, Guid churchId, CancellationToken ct = default)
    {
        var profile = await db.TeacherProfiles.AsNoTracking()
            .Include(t => t.ApplicationUser)
            .Include(t => t.AssignedClassGroups).ThenInclude(a => a.ClassGroup)
            .FirstOrDefaultAsync(t => t.ApplicationUserId == userId && t.ChurchId == churchId, ct);

        if (profile is null)
            return new TeacherWorkspaceDto(string.Empty, []);

        var classGroupIds = profile.AssignedClassGroups.Select(a => a.ClassGroupId).ToList();

        // Children in the teacher's classes.
        var children = await db.Children.AsNoTracking()
            .Where(c => classGroupIds.Contains(c.ClassGroupId) && c.IsActive)
            .OrderBy(c => c.FirstName).ThenBy(c => c.LastName)
            .Select(c => new { c.Id, c.ClassGroupId, c.FirstName, c.LastName, c.DateOfBirth, c.AvatarId })
            .ToListAsync(ct);
        var childIds = children.Select(c => c.Id).ToList();

        // Published lessons assigned to the teacher's classes.
        var lessons = await db.LessonClassGroups.AsNoTracking()
            .Where(lcg => classGroupIds.Contains(lcg.ClassGroupId) && lcg.Lesson.IsPublished)
            .Select(lcg => new
            {
                lcg.ClassGroupId,
                lcg.Lesson.Id,
                lcg.Lesson.Title,
                lcg.Lesson.BibleReference,
                lcg.Lesson.Theme,
                MemoryVerseReference = lcg.Lesson.MemoryVerse != null ? lcg.Lesson.MemoryVerse.BibleReference : null,
                lcg.SortOrder
            })
            .ToListAsync(ct);

        // Completed progress for these children (lesson -> which children completed it).
        var completed = await db.ChildProgresses.AsNoTracking()
            .Where(p => childIds.Contains(p.ChildId) && p.CompletedAtUtc != null)
            .Select(p => new { p.ChildId, p.LessonId })
            .ToListAsync(ct);

        // Badges awarded to these children.
        var awarded = await db.ChildBadges.AsNoTracking()
            .Where(cb => childIds.Contains(cb.ChildId))
            .Select(cb => new { cb.ChildId, cb.BadgeId, cb.Badge.Name, cb.Badge.IconName })
            .ToListAsync(ct);

        // childId -> class, for counting completions per (class, lesson).
        var childClass = children.ToDictionary(c => c.Id, c => c.ClassGroupId);
        var today = DateOnly.FromDateTime(DateTime.Today);

        var classes = profile.AssignedClassGroups
            .Select(a => a.ClassGroup)
            .OrderBy(cg => cg.MinAge).ThenBy(cg => cg.Name)
            .Select(cg => new TeacherWorkspaceClassDto(
                cg.Id,
                cg.Name,
                cg.MinAge,
                cg.MaxAge,
                cg.Description,
                lessons.Where(l => l.ClassGroupId == cg.Id)
                    // Already-taught lessons sink to the bottom; the rest stay in teaching order.
                    .OrderBy(l => completed.Any(p => p.LessonId == l.Id && childClass.TryGetValue(p.ChildId, out var g) && g == cg.Id) ? 1 : 0)
                    .ThenBy(l => l.SortOrder).ThenBy(l => l.Title)
                    .Select(l => new TeacherWorkspaceLessonDto(
                        l.Id, l.Title, l.BibleReference, l.Theme, l.MemoryVerseReference,
                        completed.Count(p => p.LessonId == l.Id && childClass.TryGetValue(p.ChildId, out var gid) && gid == cg.Id)))
                    .ToList(),
                children.Where(c => c.ClassGroupId == cg.Id)
                    .Select(c => new TeacherWorkspaceChildDto(
                        c.Id, c.FirstName, c.LastName, AgeAt(c.DateOfBirth, today), c.AvatarId,
                        awarded.Where(b => b.ChildId == c.Id)
                            .GroupBy(b => new { b.BadgeId, b.Name, b.IconName })
                            .Select(g => new TeacherWorkspaceBadgeDto(g.Key.BadgeId, g.Key.Name, g.Key.IconName, g.Count()))
                            .ToList()))
                    .ToList()))
            .ToList();

        return new TeacherWorkspaceDto(profile.ApplicationUser.FullName, classes);
    }

    public async Task<int> MarkLessonCompletedAsync(Guid churchId, Guid lessonId, Guid classGroupId, DateOnly date, IReadOnlyList<Guid> verseChildIds, CancellationToken ct = default)
    {
        await EnsureAssignedToClassAsync(churchId, classGroupId, ct);

        var lessonAssigned = await db.LessonClassGroups.AsNoTracking()
            .AnyAsync(lcg => lcg.LessonId == lessonId && lcg.ClassGroupId == classGroupId
                             && lcg.Lesson.ChurchId == churchId && lcg.Lesson.IsPublished, ct);
        if (!lessonAssigned)
            throw new ValidationException("That lesson is not assigned to this class group.");

        var presentChildIds = await db.Attendances.AsNoTracking()
            .Where(a => a.ClassGroupId == classGroupId && a.AttendanceDate == date && a.IsPresent)
            .Select(a => a.ChildId)
            .ToListAsync(ct);

        if (presentChildIds.Count == 0)
            throw new ValidationException("No children were marked present for this class on that date. Take attendance first.");

        var existing = await db.ChildProgresses
            .Where(p => p.LessonId == lessonId && presentChildIds.Contains(p.ChildId))
            .ToDictionaryAsync(p => p.ChildId, ct);

        // Only present children can be credited with the verse.
        var verseSet = verseChildIds.Where(presentChildIds.Contains).ToHashSet();

        var now = DateTime.UtcNow;
        foreach (var childId in presentChildIds)
        {
            var learnedVerse = verseSet.Contains(childId);
            if (existing.TryGetValue(childId, out var progress))
            {
                progress.CompletedAtUtc ??= now;
                if (learnedVerse) progress.MemoryVerseCompleted = true;
            }
            else
            {
                db.ChildProgresses.Add(new ChildProgress
                {
                    ChildId = childId,
                    LessonId = lessonId,
                    CompletedAtUtc = now,
                    MemoryVerseCompleted = learnedVerse
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return presentChildIds.Count;
    }

    public async Task<List<TeacherBadgeDto>> GetBadgeCatalogAsync(Guid churchId, CancellationToken ct = default)
    {
        // Teachers award day-to-day badges; Achievement-tier ones are reserved for ChurchAdmins.
        return await db.Badges.AsNoTracking()
            .Where(b => b.ChurchId == churchId && b.IsActive && b.Tier == Grained.Domain.Enums.BadgeTier.Standard)
            .OrderBy(b => b.Name)
            .Select(b => new TeacherBadgeDto(b.Id, b.Name, b.Description, b.IconName))
            .ToListAsync(ct);
    }

    public async Task AwardBadgeAsync(Guid churchId, Guid childId, Guid badgeId, CancellationToken ct = default)
    {
        var child = await db.Children.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId && c.ChurchId == churchId, ct)
            ?? throw new ValidationException("Child not found.");

        await EnsureAssignedToClassAsync(churchId, child.ClassGroupId, ct);

        var badge = await db.Badges.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == badgeId && b.ChurchId == churchId && b.IsActive, ct)
            ?? throw new ValidationException("Badge not found.");
        if (badge.Tier != Grained.Domain.Enums.BadgeTier.Standard)
            throw new ValidationException("Achievements can only be awarded by a church admin.");

        // One-time badges can't be re-awarded; repeatable ones always add a new award.
        if (!badge.Repeatable && await db.ChildBadges.AnyAsync(cb => cb.ChildId == childId && cb.BadgeId == badgeId, ct))
            throw new ValidationException("This child already has that badge, and it can only be awarded once.");

        db.ChildBadges.Add(new ChildBadge { ChildId = childId, BadgeId = badgeId });
        await db.SaveChangesAsync(ct);
    }

    // A teacher may only act on classes they're assigned to; admins are unrestricted.
    private async Task EnsureAssignedToClassAsync(Guid churchId, Guid classGroupId, CancellationToken ct)
    {
        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);
        if (scope is not null && !scope.Contains(classGroupId))
            throw new ValidationException("You are not assigned to this class group.");
    }

    private static int AgeAt(DateOnly dob, DateOnly today)
    {
        var age = today.Year - dob.Year;
        if (dob > today.AddYears(-age))
            age--;
        return age;
    }
}
