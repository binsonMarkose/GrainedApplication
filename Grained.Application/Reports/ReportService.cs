using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Application.Growth;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Reports;

public class ReportService(IApplicationDbContext db, IGrowthService growth, ITeacherScope teacherScope) : IReportService
{
    public async Task<List<ChildProgressReportRow>> GetChildProgressReportAsync(Guid churchId, CancellationToken ct = default)
    {
        // A plain Teacher only sees children in their assigned class groups; admins see the whole church.
        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);

        var query = db.Children.AsNoTracking()
            .Include(c => c.ClassGroup)
            .Include(c => c.ChildProgresses)
            .Where(c => c.ChurchId == churchId && c.IsActive);
        if (scope is not null)
            query = query.Where(c => scope.Contains(c.ClassGroupId));

        var children = await query
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .ToListAsync(ct);

        var growthById = await growth.GetGrowthForChildrenAsync(children.Select(c => c.Id).ToList(), churchId, ct);

        return children.Select(c =>
        {
            var completed = c.ChildProgresses.Where(p => p.CompletedAtUtc is not null).ToList();
            var scores = completed.Where(p => p.QuizScore is not null).Select(p => (double)p.QuizScore!.Value).ToList();
            growthById.TryGetValue(c.Id, out var g);
            return new ChildProgressReportRow(
                c.Id, $"{c.FirstName} {c.LastName}", c.AvatarId, c.ClassGroup.Name,
                g?.StageIndex ?? 0, g?.StageName ?? "Seed", g?.StageEmoji ?? "🌰", g?.GrowthPoints ?? 0,
                g?.LessonsCompleted ?? completed.Count, g?.VersesLearned ?? 0, g?.SundaysAttended ?? 0,
                g?.BadgeCount ?? 0, g?.AchievementCount ?? 0,
                scores.Count > 0 ? scores.Average() : null);
        }).ToList();
    }

    public async Task<List<ChildBadgeReportRow>> GetChildBadgesAsync(Guid childId, Guid churchId, CancellationToken ct = default)
    {
        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);
        var child = await db.Children.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId && c.ChurchId == churchId, ct);
        // Hide out-of-scope children behind the same "not found" as another church's child.
        if (child is null || (scope is not null && !scope.Contains(child.ClassGroupId)))
            throw new ValidationException("Child not found.");

        return await db.ChildBadges.AsNoTracking()
            .Where(cb => cb.ChildId == childId && cb.Badge.ChurchId == churchId)
            .OrderByDescending(cb => cb.AwardedAtUtc)
            .Select(cb => new ChildBadgeReportRow(
                cb.BadgeId, cb.Badge.Name, cb.Badge.Description, cb.Badge.IconName,
                (int)cb.Badge.Tier, cb.Badge.Points, cb.AwardedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<List<ClassProgressReportRow>> GetClassProgressReportAsync(Guid churchId, CancellationToken ct = default)
    {
        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);

        var cgQuery = db.ClassGroups.AsNoTracking()
            .Include(c => c.Children.Where(ch => ch.IsActive)).ThenInclude(ch => ch.ChildProgresses)
            .Where(c => c.ChurchId == churchId && c.IsActive);
        if (scope is not null)
            cgQuery = cgQuery.Where(c => scope.Contains(c.Id));

        var classGroups = await cgQuery
            .OrderBy(c => c.MinAge)
            .ToListAsync(ct);

        var publishedLessonCount = await db.Lessons.AsNoTracking()
            .CountAsync(l => l.ChurchId == churchId && l.IsPublished, ct);

        return classGroups.Select(cg =>
        {
            var totalChildren = cg.Children.Count;
            var totalCompleted = cg.Children.Sum(c => c.ChildProgresses.Count(p => p.CompletedAtUtc is not null));
            var possibleCompletions = totalChildren * Math.Max(publishedLessonCount, 1);
            var rate = possibleCompletions == 0 ? 0 : (double)totalCompleted / possibleCompletions * 100;

            return new ClassProgressReportRow(cg.Id, cg.Name, totalChildren, totalCompleted, Math.Round(rate, 1));
        }).ToList();
    }

    public async Task<List<AttendanceReportRow>> GetAttendanceReportAsync(Guid churchId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);

        var cgQuery = db.ClassGroups.AsNoTracking()
            .Where(c => c.ChurchId == churchId && c.IsActive);
        if (scope is not null)
            cgQuery = cgQuery.Where(c => scope.Contains(c.Id));

        var classGroups = await cgQuery
            .OrderBy(c => c.MinAge)
            .ToListAsync(ct);

        var attendanceQuery = db.Attendances.AsNoTracking()
            .Where(a => a.ClassGroup.ChurchId == churchId && a.AttendanceDate >= from && a.AttendanceDate <= to);
        if (scope is not null)
            attendanceQuery = attendanceQuery.Where(a => scope.Contains(a.ClassGroupId));

        var attendance = await attendanceQuery.ToListAsync(ct);

        return classGroups.Select(cg =>
        {
            var records = attendance.Where(a => a.ClassGroupId == cg.Id).ToList();
            var present = records.Count(a => a.IsPresent);
            var total = records.Count;
            var rate = total == 0 ? 0 : (double)present / total * 100;

            return new AttendanceReportRow(cg.Id, cg.Name, total, present, total - present, Math.Round(rate, 1));
        }).ToList();
    }

    public async Task<List<LessonCompletionReportRow>> GetLessonCompletionReportAsync(Guid churchId, CancellationToken ct = default)
    {
        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);

        var query = db.Lessons.AsNoTracking()
            .Include(l => l.ChildProgresses).ThenInclude(p => p.Child)
            .Where(l => l.ChurchId == churchId);
        // A teacher only sees lessons assigned to one of their class groups.
        if (scope is not null)
            query = query.Where(l => l.AssignedClassGroups.Any(a => scope.Contains(a.ClassGroupId)));

        var lessons = await query
            .OrderByDescending(l => l.CreatedAtUtc)
            .ToListAsync(ct);

        return lessons.Select(l =>
        {
            // Count only completions from children the teacher can see.
            var completed = l.ChildProgresses
                .Where(p => p.CompletedAtUtc is not null && (scope is null || scope.Contains(p.Child.ClassGroupId)))
                .ToList();
            var scores = completed.Where(p => p.QuizScore is not null).Select(p => (double)p.QuizScore!.Value).ToList();
            return new LessonCompletionReportRow(l.Id, l.Title, l.IsPublished, completed.Count,
                scores.Count > 0 ? scores.Average() : null);
        }).ToList();
    }
}
