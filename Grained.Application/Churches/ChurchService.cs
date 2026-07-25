using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Churches;

public class ChurchService(IApplicationDbContext db) : IChurchService
{
    public async Task<List<ChurchDto>> GetAllAsync(bool includeInactive = false, Grained.Domain.Enums.ChurchStatus? status = null, CancellationToken ct = default)
    {
        var query = db.Churches.AsNoTracking().AsQueryable();
        if (!includeInactive)
            query = query.Where(c => c.IsActive);
        if (status is not null)
            query = query.Where(c => c.Status == status);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new ChurchDto(c.Id, c.Name, c.Address, c.Email, c.Phone, c.IsActive, c.Status.ToString(), c.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<ChurchDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await db.Churches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? null : new ChurchDto(c.Id, c.Name, c.Address, c.Email, c.Phone, c.IsActive, c.Status.ToString(), c.CreatedAtUtc);
    }

    public async Task<Guid> CreateAsync(ChurchFormModel model, CancellationToken ct = default)
    {
        var church = new Church
        {
            Name = model.Name.Trim(),
            Address = model.Address?.Trim(),
            Email = model.Email.Trim(),
            Phone = model.Phone?.Trim()
        };
        db.Churches.Add(church);
        await db.SaveChangesAsync(ct);
        return church.Id;
    }

    public async Task UpdateAsync(ChurchFormModel model, CancellationToken ct = default)
    {
        if (model.Id is null)
            throw new ValidationException("Church id is required for update.");

        var church = await db.Churches.FirstOrDefaultAsync(x => x.Id == model.Id, ct)
            ?? throw new ValidationException("Church not found.");

        church.Name = model.Name.Trim();
        church.Address = model.Address?.Trim();
        church.Email = model.Email.Trim();
        church.Phone = model.Phone?.Trim();

        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var church = await db.Churches.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ValidationException("Church not found.");
        church.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }
}
