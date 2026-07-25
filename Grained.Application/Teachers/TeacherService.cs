using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Common;
using Grained.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Teachers;

public class TeacherService(IApplicationDbContext db, UserManager<ApplicationUser> userManager) : ITeacherService
{
    public async Task<List<TeacherDto>> GetForChurchAsync(Guid churchId, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = db.TeacherProfiles.AsNoTracking()
            .Include(t => t.ApplicationUser)
            .Include(t => t.AssignedClassGroups).ThenInclude(a => a.ClassGroup)
            .Where(t => t.ChurchId == churchId);

        if (!includeInactive)
            query = query.Where(t => t.IsActive);

        var teachers = await query.OrderBy(t => t.ApplicationUser.FullName).ToListAsync(ct);
        return teachers.Select(ToDto).ToList();
    }

    public async Task<TeacherDto?> GetByIdAsync(Guid teacherProfileId, Guid churchId, CancellationToken ct = default)
    {
        var teacher = await db.TeacherProfiles.AsNoTracking()
            .Include(t => t.ApplicationUser)
            .Include(t => t.AssignedClassGroups).ThenInclude(a => a.ClassGroup)
            .FirstOrDefaultAsync(t => t.Id == teacherProfileId && t.ChurchId == churchId, ct);

        return teacher is null ? null : ToDto(teacher);
    }

    public async Task<(Guid TeacherProfileId, string TemporaryPassword)> CreateAsync(Guid churchId, TeacherFormModel model, CancellationToken ct = default)
    {
        if (await userManager.FindByEmailAsync(model.Email.Trim()) is not null)
            throw new ValidationException("A user with this email already exists.");

        await EnsureClassGroupsBelongToChurch(model.AssignedClassGroupIds, churchId, ct);

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            FullName = model.FullName.Trim(),
            ChurchId = churchId,
            MustChangePassword = true // login code is temporary; teacher sets their own on first sign-in
        };

        var temporaryPassword = GenerateTemporaryPassword();
        var createResult = await userManager.CreateAsync(user, temporaryPassword);
        if (!createResult.Succeeded)
            throw new ValidationException(string.Join(" ", createResult.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, Roles.Teacher);

        var profile = new TeacherProfile { ApplicationUserId = user.Id, ChurchId = churchId };
        db.TeacherProfiles.Add(profile);
        await db.SaveChangesAsync(ct);

        foreach (var classGroupId in model.AssignedClassGroupIds)
        {
            db.TeacherClassGroups.Add(new TeacherClassGroup { TeacherProfileId = profile.Id, ClassGroupId = classGroupId });
        }
        await db.SaveChangesAsync(ct);

        return (profile.Id, temporaryPassword);
    }

    public async Task UpdateAsync(Guid churchId, TeacherFormModel model, CancellationToken ct = default)
    {
        if (model.TeacherProfileId is null)
            throw new ValidationException("Teacher id is required for update.");

        await EnsureClassGroupsBelongToChurch(model.AssignedClassGroupIds, churchId, ct);

        var profile = await db.TeacherProfiles
            .Include(t => t.ApplicationUser)
            .Include(t => t.AssignedClassGroups)
            .FirstOrDefaultAsync(t => t.Id == model.TeacherProfileId && t.ChurchId == churchId, ct)
            ?? throw new ValidationException("Teacher not found.");

        profile.ApplicationUser.FullName = model.FullName.Trim();

        var toRemove = profile.AssignedClassGroups.Where(a => !model.AssignedClassGroupIds.Contains(a.ClassGroupId)).ToList();
        foreach (var assignment in toRemove)
            db.TeacherClassGroups.Remove(assignment);

        var existingIds = profile.AssignedClassGroups.Select(a => a.ClassGroupId).ToHashSet();
        foreach (var classGroupId in model.AssignedClassGroupIds.Where(id => !existingIds.Contains(id)))
            db.TeacherClassGroups.Add(new TeacherClassGroup { TeacherProfileId = profile.Id, ClassGroupId = classGroupId });

        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(Guid teacherProfileId, Guid churchId, bool isActive, CancellationToken ct = default)
    {
        var profile = await db.TeacherProfiles
            .Include(t => t.ApplicationUser)
            .FirstOrDefaultAsync(t => t.Id == teacherProfileId && t.ChurchId == churchId, ct)
            ?? throw new ValidationException("Teacher not found.");

        profile.IsActive = isActive;
        profile.ApplicationUser.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid teacherProfileId, Guid churchId, CancellationToken ct = default)
    {
        var profile = await db.TeacherProfiles
            .FirstOrDefaultAsync(t => t.Id == teacherProfileId && t.ChurchId == churchId, ct)
            ?? throw new ValidationException("Teacher not found.");

        var user = await userManager.FindByIdAsync(profile.ApplicationUserId.ToString());
        var roles = user is not null ? await userManager.GetRolesAsync(user) : new List<string>();
        var hasOtherRole = roles.Any(r => r != Roles.Teacher);
        var isParent = await db.Children.AnyAsync(c => c.ParentUserId == profile.ApplicationUserId, ct);

        if (user is not null && !hasOtherRole && !isParent)
        {
            // Solely a teacher — remove the account entirely. TeacherProfile (and its class-group
            // assignments) cascade from the user delete. Authored lessons keep their AuthorName snapshot.
            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
                throw new ValidationException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }
        else
        {
            // Dual-role (admin) or a linked parent — keep the login, just drop the teacher role + profile.
            if (user is not null && roles.Contains(Roles.Teacher))
                await userManager.RemoveFromRoleAsync(user, Roles.Teacher);
            db.TeacherProfiles.Remove(profile);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<string> ResetLoginCodeAsync(Guid teacherProfileId, Guid churchId, CancellationToken ct = default)
    {
        var profile = await db.TeacherProfiles.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == teacherProfileId && t.ChurchId == churchId, ct)
            ?? throw new ValidationException("Teacher not found.");

        // Operate on the UserManager's own tracked instance.
        var user = await userManager.FindByIdAsync(profile.ApplicationUserId.ToString())
            ?? throw new ValidationException("Teacher account not found.");

        var newCode = GenerateTemporaryPassword();
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newCode);
        if (!result.Succeeded)
            throw new ValidationException(string.Join(" ", result.Errors.Select(e => e.Description)));

        // Reset codes are temporary too — force a fresh personal password on next sign-in.
        user.MustChangePassword = true;
        await userManager.UpdateAsync(user);

        return newCode;
    }

    private async Task EnsureClassGroupsBelongToChurch(List<Guid> classGroupIds, Guid churchId, CancellationToken ct)
    {
        if (classGroupIds.Count == 0)
            return;

        var validCount = await db.ClassGroups.AsNoTracking()
            .CountAsync(c => classGroupIds.Contains(c.Id) && c.ChurchId == churchId, ct);

        if (validCount != classGroupIds.Distinct().Count())
            throw new ValidationException("One or more selected class groups do not belong to this church.");
    }

    // A short, human-friendly login code the ChurchAdmin can read aloud/hand to the teacher.
    // Still satisfies Identity's default policy (upper + lower + digit + non-alphanumeric, len >= 6):
    // capital "G" + lowercase letters + "-" + 5 digits, e.g. "Grained-48213".
    private static string GenerateTemporaryPassword() =>
        $"Grained-{Random.Shared.Next(10000, 100000)}";

    private static TeacherDto ToDto(TeacherProfile t) => new(
        t.Id, t.ApplicationUserId, t.ApplicationUser.FullName, t.ApplicationUser.Email ?? string.Empty, t.IsActive,
        t.AssignedClassGroups.Select(a => a.ClassGroupId).ToList(),
        t.AssignedClassGroups.Select(a => a.ClassGroup.Name).ToList());
}
