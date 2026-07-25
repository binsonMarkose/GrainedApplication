using Grained.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Dashboard;

public class DashboardService(IApplicationDbContext db) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(Guid churchId, CancellationToken ct = default)
    {
        var totalChildren = await db.Children.AsNoTracking().CountAsync(c => c.ChurchId == churchId && c.IsActive, ct);
        var totalClasses = await db.ClassGroups.AsNoTracking().CountAsync(c => c.ChurchId == churchId && c.IsActive, ct);
        var publishedLessons = await db.Lessons.AsNoTracking().CountAsync(l => l.ChurchId == churchId && l.IsPublished, ct);
        var totalTeachers = await db.Users.AsNoTracking()
            .CountAsync(u => u.ChurchId == churchId && u.IsActive && u.TeacherProfile != null, ct);

        var recentAttendance = await db.Attendances.AsNoTracking()
            .Include(a => a.Child)
            .Include(a => a.ClassGroup)
            .Where(a => a.ClassGroup.ChurchId == churchId)
            .OrderByDescending(a => a.AttendanceDate)
            .Take(10)
            .Select(a => new RecentAttendanceDto($"{a.Child.FirstName} {a.Child.LastName}", a.ClassGroup.Name, a.AttendanceDate, a.IsPresent))
            .ToListAsync(ct);

        var recentCompletions = await db.ChildProgresses.AsNoTracking()
            .Include(p => p.Child)
            .Include(p => p.Lesson)
            .Where(p => p.Lesson.ChurchId == churchId && p.CompletedAtUtc != null)
            .OrderByDescending(p => p.CompletedAtUtc)
            .Take(10)
            .Select(p => new RecentCompletionDto($"{p.Child.FirstName} {p.Child.LastName}", p.Lesson.Title, p.CompletedAtUtc!.Value))
            .ToListAsync(ct);

        return new DashboardSummaryDto(totalChildren, totalTeachers, totalClasses, publishedLessons, recentAttendance, recentCompletions);
    }
}
