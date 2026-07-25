using Grained.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Common.Services;

public class TeacherScope(IApplicationDbContext db, ICurrentUserService currentUser) : ITeacherScope
{
    public async Task<List<Guid>?> GetAssignedClassGroupIdsAsync(Guid churchId, CancellationToken ct = default)
    {
        // Admins and super admins see the whole church; only a plain Teacher is scoped to their classes.
        if (currentUser.IsChurchAdmin || currentUser.IsSuperAdmin || !currentUser.IsTeacher)
            return null;

        var userId = currentUser.UserId;
        return await db.TeacherClassGroups.AsNoTracking()
            .Where(tcg => tcg.TeacherProfile.ApplicationUserId == userId && tcg.TeacherProfile.ChurchId == churchId)
            .Select(tcg => tcg.ClassGroupId)
            .ToListAsync(ct);
    }
}
