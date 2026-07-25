using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Common;
using Grained.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Children;

public class ChildService(IApplicationDbContext db, ITeacherScope teacherScope, UserManager<ApplicationUser> userManager)
    : IChildService
{
    public async Task<List<ChildDto>> GetForChurchAsync(Guid churchId, ChildFilter filter, CancellationToken ct = default)
    {
        var query = db.Children.AsNoTracking()
            .Include(c => c.ClassGroup)
            .Where(c => c.ChurchId == churchId);

        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);
        if (scope is not null)
            query = query.Where(c => scope.Contains(c.ClassGroupId));

        if (filter.ClassGroupId is not null)
            query = query.Where(c => c.ClassGroupId == filter.ClassGroupId);

        if (filter.IsActive is not null)
            query = query.Where(c => c.IsActive == filter.IsActive);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var children = await query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToListAsync(ct);

        var result = children.Select(c => ToDto(c, today)).ToList();

        if (filter.MinAge is not null)
            result = result.Where(c => c.Age >= filter.MinAge).ToList();
        if (filter.MaxAge is not null)
            result = result.Where(c => c.Age <= filter.MaxAge).ToList();

        return result;
    }

    public async Task<ChildDto?> GetByIdAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var child = await db.Children.AsNoTracking()
            .Include(c => c.ClassGroup)
            .FirstOrDefaultAsync(c => c.Id == id && c.ChurchId == churchId, ct);
        if (child is null)
            return null;

        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);
        if (scope is not null && !scope.Contains(child.ClassGroupId))
            return null;

        return ToDto(child, DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task<Guid> CreateAsync(Guid churchId, ChildFormModel model, CancellationToken ct = default)
    {
        await EnsureClassGroupBelongsToChurch(model.ClassGroupId!.Value, churchId, ct);

        var child = new Child
        {
            ChurchId = churchId,
            ClassGroupId = model.ClassGroupId.Value,
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            DateOfBirth = model.DateOfBirth,
            ParentName = model.ParentName.Trim(),
            ParentEmail = model.ParentEmail.Trim(),
            ParentPhone = model.ParentPhone?.Trim()
        };
        // If an account already exists for this email, link the child (and promote the account to
        // Parent) straight away so it shows in the parent view.
        child.ParentUserId = await LinkParentAccountAsync(churchId, child.ParentEmail, ct);
        db.Children.Add(child);
        await db.SaveChangesAsync(ct);
        return child.Id;
    }

    public async Task UpdateAsync(Guid churchId, ChildFormModel model, CancellationToken ct = default)
    {
        if (model.Id is null)
            throw new ValidationException("Child id is required for update.");

        await EnsureClassGroupBelongsToChurch(model.ClassGroupId!.Value, churchId, ct);

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == model.Id && c.ChurchId == churchId, ct)
            ?? throw new ValidationException("Child not found.");

        child.FirstName = model.FirstName.Trim();
        child.LastName = model.LastName.Trim();
        child.DateOfBirth = model.DateOfBirth;
        child.ClassGroupId = model.ClassGroupId.Value;
        child.ParentName = model.ParentName.Trim();
        child.ParentEmail = model.ParentEmail.Trim();
        child.ParentPhone = model.ParentPhone?.Trim();
        // Re-link to the account matching the (possibly changed) email, promoting it to Parent.
        child.ParentUserId = await LinkParentAccountAsync(churchId, child.ParentEmail, ct);

        await db.SaveChangesAsync(ct);
    }

    // Links a child to the account matching its parent email (in this church), if one exists. Grants
    // the Parent role when the account doesn't have it yet — so linking to a teacher/admin promotes
    // them to a dual (staff + parent) account. All children sharing the email are linked. Returns the
    // account id, or null when no account exists yet (a brand-new email is provisioned later via the
    // Parent-code action). The admin confirms in the UI before this promotes an existing account.
    private async Task<Guid?> LinkParentAccountAsync(Guid churchId, string parentEmail, CancellationToken ct)
    {
        var email = parentEmail.Trim();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null || (user.ChurchId is not null && user.ChurchId != churchId))
            return null;

        if (!await userManager.IsInRoleAsync(user, Roles.Parent))
            await userManager.AddToRoleAsync(user, Roles.Parent);

        // Link every child already sharing this email (siblings) to the account.
        var siblings = await db.Children
            .Where(c => c.ChurchId == churchId && c.ParentEmail == email)
            .ToListAsync(ct);
        foreach (var sibling in siblings)
            sibling.ParentUserId = user.Id;

        return user.Id;
    }

    public async Task<ParentLookupResult> LookupParentAsync(Guid churchId, string email, CancellationToken ct = default)
    {
        _ = ct;
        var trimmed = (email ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return new ParentLookupResult(false, null, false, false);

        var user = await userManager.FindByEmailAsync(trimmed);
        if (user is null || (user.ChurchId is not null && user.ChurchId != churchId))
            return new ParentLookupResult(false, null, false, false);

        var roles = await userManager.GetRolesAsync(user);
        var isStaff = roles.Any(r => r is Roles.SuperAdmin or Roles.ChurchAdmin or Roles.Teacher);
        return new ParentLookupResult(true, user.FullName, isStaff, roles.Contains(Roles.Parent));
    }

    public async Task AssignClassGroupAsync(Guid id, Guid churchId, Guid classGroupId, CancellationToken ct = default)
    {
        await EnsureClassGroupBelongsToChurch(classGroupId, churchId, ct);

        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == id && c.ChurchId == churchId, ct)
            ?? throw new ValidationException("Child not found.");

        child.ClassGroupId = classGroupId;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(Guid id, Guid churchId, bool isActive, CancellationToken ct = default)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == id && c.ChurchId == churchId, ct)
            ?? throw new ValidationException("Child not found.");
        child.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var child = await db.Children.FirstOrDefaultAsync(c => c.Id == id && c.ChurchId == churchId, ct)
            ?? throw new ValidationException("Child not found.");

        // Badges, lesson progress and attendance cascade with the child. The parent login account is
        // left intact (they may have siblings or be re-linked later).
        db.Children.Remove(child);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ParentCodeResult> CreateOrResetParentCodeAsync(Guid childId, Guid churchId, CancellationToken ct = default)
    {
        var child = await db.Children.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId && c.ChurchId == churchId, ct)
            ?? throw new ValidationException("Child not found.");

        var email = child.ParentEmail.Trim();
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("This child has no parent email on file.");

        var user = await userManager.FindByEmailAsync(email);
        string? code = null;
        var linkedExisting = false;

        if (user is null)
        {
            // Brand-new parent account: issue a login code.
            code = GenerateParentCode();
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = string.IsNullOrWhiteSpace(child.ParentName) ? email : child.ParentName.Trim(),
                ChurchId = churchId,
                MustChangePassword = true // login code is temporary; parent sets their own on first sign-in
            };
            var createResult = await userManager.CreateAsync(user, code);
            if (!createResult.Succeeded)
                throw new ValidationException(string.Join(" ", createResult.Errors.Select(e => e.Description)));
            await userManager.AddToRoleAsync(user, Roles.Parent);
        }
        else
        {
            // The email already has an account. Accounts are single-church and single-credential,
            // so we never mint a second code — we just grant the Parent role and link the children.
            if (user.ChurchId is not null && user.ChurchId != churchId)
                throw new ValidationException("That email belongs to an account in another church.");

            var roles = await userManager.GetRolesAsync(user);
            var isStaff = roles.Any(r => r is Roles.SuperAdmin or Roles.ChurchAdmin or Roles.Teacher);

            if (!roles.Contains(Roles.Parent))
                await userManager.AddToRoleAsync(user, Roles.Parent);

            if (isStaff)
            {
                // Dual role: keep their existing (staff) login untouched.
                linkedExisting = true;
            }
            else
            {
                // Parent-only account: safe to reset the code so the admin can re-share it.
                code = GenerateParentCode();
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetResult = await userManager.ResetPasswordAsync(user, token, code);
                if (!resetResult.Succeeded)
                    throw new ValidationException(string.Join(" ", resetResult.Errors.Select(e => e.Description)));
                user.MustChangePassword = true; // temporary code → parent sets their own on next sign-in
                await userManager.UpdateAsync(user);
            }
        }

        // Link this child and any siblings sharing the parent email to the parent account.
        var siblings = await db.Children
            .Where(c => c.ChurchId == churchId && c.ParentEmail == child.ParentEmail)
            .ToListAsync(ct);
        foreach (var sibling in siblings)
            sibling.ParentUserId = user.Id;
        await db.SaveChangesAsync(ct);

        return new ParentCodeResult(code, linkedExisting, email);
    }

    // Same friendly, policy-satisfying shape as the teacher login code.
    private static string GenerateParentCode() => $"Grained-{Random.Shared.Next(10000, 100000)}";

    private async Task EnsureClassGroupBelongsToChurch(Guid classGroupId, Guid churchId, CancellationToken ct)
    {
        var exists = await db.ClassGroups.AsNoTracking().AnyAsync(c => c.Id == classGroupId && c.ChurchId == churchId, ct);
        if (!exists)
            throw new ValidationException("Selected class group does not belong to this church.");
    }

    private static ChildDto ToDto(Child c, DateOnly today)
    {
        var age = today.Year - c.DateOfBirth.Year;
        if (c.DateOfBirth > today.AddYears(-age))
            age--;

        return new ChildDto(c.Id, c.ChurchId, c.ClassGroupId, c.ClassGroup.Name, c.FirstName, c.LastName,
            c.DateOfBirth, age, c.ParentName, c.ParentEmail, c.ParentPhone, c.IsActive);
    }
}
