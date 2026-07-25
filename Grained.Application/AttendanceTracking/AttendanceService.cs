using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.AttendanceTracking;

public class AttendanceService(IApplicationDbContext db, ICurrentUserService currentUser) : IAttendanceService
{
    public async Task<List<AttendanceRosterEntryDto>> GetRosterAsync(Guid churchId, Guid classGroupId, DateOnly date, CancellationToken ct = default)
    {
        var classGroupExists = await db.ClassGroups.AsNoTracking()
            .AnyAsync(c => c.Id == classGroupId && c.ChurchId == churchId, ct);
        if (!classGroupExists)
            throw new ValidationException("Class group not found.");

        await EnsureTeacherAssignedAsync(churchId, classGroupId, ct);

        var children = await db.Children.AsNoTracking()
            .Where(c => c.ClassGroupId == classGroupId && c.ChurchId == churchId && c.IsActive)
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .ToListAsync(ct);

        var existing = await db.Attendances.AsNoTracking()
            .Where(a => a.ClassGroupId == classGroupId && a.AttendanceDate == date)
            .ToDictionaryAsync(a => a.ChildId, ct);

        return children.Select(c =>
        {
            existing.TryGetValue(c.Id, out var record);
            return new AttendanceRosterEntryDto(c.Id, c.FirstName, c.LastName, record?.IsPresent ?? false, record?.Notes);
        }).ToList();
    }

    public async Task SaveAsync(Guid churchId, AttendanceSaveModel model, CancellationToken ct = default)
    {
        var classGroupExists = await db.ClassGroups.AsNoTracking()
            .AnyAsync(c => c.Id == model.ClassGroupId && c.ChurchId == churchId, ct);
        if (!classGroupExists)
            throw new ValidationException("Class group not found.");

        await EnsureTeacherAssignedAsync(churchId, model.ClassGroupId, ct);

        var childIds = model.Entries.Select(e => e.ChildId).ToList();
        var validChildIds = await db.Children.AsNoTracking()
            .Where(c => childIds.Contains(c.Id) && c.ChurchId == churchId)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var existingRecords = await db.Attendances
            .Where(a => a.ClassGroupId == model.ClassGroupId && a.AttendanceDate == model.AttendanceDate
                        && childIds.Contains(a.ChildId))
            .ToListAsync(ct);

        foreach (var entry in model.Entries)
        {
            if (!validChildIds.Contains(entry.ChildId))
                continue;

            var record = existingRecords.FirstOrDefault(a => a.ChildId == entry.ChildId);
            if (record is null)
            {
                db.Attendances.Add(new Domain.Entities.Attendance
                {
                    ChildId = entry.ChildId,
                    ClassGroupId = model.ClassGroupId,
                    LessonId = model.LessonId,
                    AttendanceDate = model.AttendanceDate,
                    IsPresent = entry.IsPresent,
                    Notes = entry.Notes?.Trim()
                });
            }
            else
            {
                record.LessonId = model.LessonId;
                record.IsPresent = entry.IsPresent;
                record.Notes = entry.Notes?.Trim();
            }
        }

        await db.SaveChangesAsync(ct);
    }

    // A ChurchAdmin can take attendance for any class in their church; a Teacher is limited to
    // the classes they've been assigned to. (Church isolation is already enforced by churchId.)
    private async Task EnsureTeacherAssignedAsync(Guid churchId, Guid classGroupId, CancellationToken ct)
    {
        if (currentUser.IsChurchAdmin || currentUser.IsSuperAdmin || !currentUser.IsTeacher)
            return;

        var userId = currentUser.UserId;
        var assigned = await db.TeacherClassGroups.AsNoTracking()
            .AnyAsync(tcg => tcg.ClassGroupId == classGroupId
                             && tcg.TeacherProfile.ApplicationUserId == userId
                             && tcg.TeacherProfile.ChurchId == churchId, ct);

        if (!assigned)
            throw new ValidationException("You are not assigned to this class group.");
    }
}
