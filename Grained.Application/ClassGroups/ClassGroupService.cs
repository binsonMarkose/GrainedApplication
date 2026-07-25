using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.ClassGroups;

public class ClassGroupService(IApplicationDbContext db, ITeacherScope teacherScope) : IClassGroupService
{
    public async Task<List<ClassGroupDto>> GetAllForChurchAsync(Guid churchId, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = db.ClassGroups.AsNoTracking().Where(c => c.ChurchId == churchId);
        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);
        if (scope is not null)
            query = query.Where(c => scope.Contains(c.Id));

        return await query
            .OrderBy(c => c.MinAge)
            .Select(c => new ClassGroupDto(c.Id, c.ChurchId, c.Name, c.MinAge, c.MaxAge, c.Description, c.IsActive,
                c.Children.Count(ch => ch.IsActive)))
            .ToListAsync(ct);
    }

    public async Task<ClassGroupDto?> GetByIdAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var scope = await teacherScope.GetAssignedClassGroupIdsAsync(churchId, ct);
        if (scope is not null && !scope.Contains(id))
            return null;

        var c = await db.ClassGroups.AsNoTracking()
            .Where(x => x.Id == id && x.ChurchId == churchId)
            .Select(x => new ClassGroupDto(x.Id, x.ChurchId, x.Name, x.MinAge, x.MaxAge, x.Description, x.IsActive,
                x.Children.Count(ch => ch.IsActive)))
            .FirstOrDefaultAsync(ct);
        return c;
    }

    public async Task<Guid> CreateAsync(Guid churchId, ClassGroupFormModel model, CancellationToken ct = default)
    {
        var classGroup = new ClassGroup
        {
            ChurchId = churchId,
            Name = model.Name.Trim(),
            MinAge = model.MinAge,
            MaxAge = model.MaxAge,
            Description = model.Description?.Trim()
        };
        db.ClassGroups.Add(classGroup);
        await db.SaveChangesAsync(ct);
        return classGroup.Id;
    }

    public async Task UpdateAsync(Guid churchId, ClassGroupFormModel model, CancellationToken ct = default)
    {
        if (model.Id is null)
            throw new ValidationException("Class group id is required for update.");

        var classGroup = await db.ClassGroups.FirstOrDefaultAsync(x => x.Id == model.Id && x.ChurchId == churchId, ct)
            ?? throw new ValidationException("Class group not found.");

        classGroup.Name = model.Name.Trim();
        classGroup.MinAge = model.MinAge;
        classGroup.MaxAge = model.MaxAge;
        classGroup.Description = model.Description?.Trim();

        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(Guid id, Guid churchId, bool isActive, CancellationToken ct = default)
    {
        var classGroup = await db.ClassGroups.FirstOrDefaultAsync(x => x.Id == id && x.ChurchId == churchId, ct)
            ?? throw new ValidationException("Class group not found.");
        classGroup.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var classGroup = await db.ClassGroups.FirstOrDefaultAsync(x => x.Id == id && x.ChurchId == churchId, ct)
            ?? throw new ValidationException("Class group not found.");

        // Children and attendance are Restrict FKs — block with a friendly message rather than a raw
        // DB error. Teacher/lesson assignments cascade, so they don't block the delete.
        if (await db.Children.AnyAsync(c => c.ClassGroupId == id, ct))
            throw new ValidationException("This class still has children in it. Move or remove them first, or disable the class instead.");
        if (await db.Attendances.AnyAsync(a => a.ClassGroupId == id, ct))
            throw new ValidationException("This class has attendance history and can’t be deleted. Disable it instead.");

        db.ClassGroups.Remove(classGroup);
        await db.SaveChangesAsync(ct);
    }
}
