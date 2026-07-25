using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Badges;

public class BadgeService(IApplicationDbContext db) : IBadgeService
{
    // Default growth points when the admin doesn't set a value:
    // a standard badge = one faithful Sunday (12); an achievement = three (36).
    private static int DefaultPoints(BadgeTier tier) => tier == BadgeTier.Achievement ? 36 : 12;

    public async Task<List<BadgeDto>> GetForChurchAsync(Guid churchId, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = db.Badges.AsNoTracking().Where(b => b.ChurchId == churchId);
        if (!includeInactive)
            query = query.Where(b => b.IsActive);

        return await query.OrderBy(b => b.Tier).ThenBy(b => b.Name)
            .Select(b => new BadgeDto(b.Id, b.ChurchId, b.Name, b.Description, b.IconName, b.Criteria, b.Tier, b.Points, b.IsActive, b.Repeatable))
            .ToListAsync(ct);
    }

    public async Task<BadgeDto?> GetByIdAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var b = await db.Badges.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.ChurchId == churchId, ct);
        return b is null ? null : new BadgeDto(b.Id, b.ChurchId, b.Name, b.Description, b.IconName, b.Criteria, b.Tier, b.Points, b.IsActive, b.Repeatable);
    }

    public async Task<Guid> CreateAsync(Guid churchId, BadgeFormModel model, CancellationToken ct = default)
    {
        var badge = new Badge
        {
            ChurchId = churchId,
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            IconName = model.IconName?.Trim(),
            Criteria = model.Criteria?.Trim(),
            Tier = model.Tier,
            Points = model.Points > 0 ? model.Points : DefaultPoints(model.Tier),
            Repeatable = model.Repeatable
        };
        db.Badges.Add(badge);
        await db.SaveChangesAsync(ct);
        return badge.Id;
    }

    public async Task UpdateAsync(Guid churchId, BadgeFormModel model, CancellationToken ct = default)
    {
        if (model.Id is null)
            throw new ValidationException("Badge id is required for update.");

        var badge = await db.Badges.FirstOrDefaultAsync(x => x.Id == model.Id && x.ChurchId == churchId, ct)
            ?? throw new ValidationException("Badge not found.");

        badge.Name = model.Name.Trim();
        badge.Description = model.Description?.Trim();
        badge.IconName = model.IconName?.Trim();
        badge.Criteria = model.Criteria?.Trim();
        badge.Tier = model.Tier;
        badge.Points = model.Points > 0 ? model.Points : DefaultPoints(model.Tier);
        badge.Repeatable = model.Repeatable;

        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(Guid id, Guid churchId, bool isActive, CancellationToken ct = default)
    {
        var badge = await db.Badges.FirstOrDefaultAsync(x => x.Id == id && x.ChurchId == churchId, ct)
            ?? throw new ValidationException("Badge not found.");
        badge.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    // Admin award: any badge (including Achievement tier) to any child in the church.
    public async Task AwardToChildAsync(Guid churchId, Guid childId, Guid badgeId, CancellationToken ct = default)
    {
        var childOk = await db.Children.AsNoTracking().AnyAsync(c => c.Id == childId && c.ChurchId == churchId, ct);
        if (!childOk)
            throw new ValidationException("Child not found.");

        var badge = await db.Badges.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == badgeId && b.ChurchId == churchId && b.IsActive, ct)
            ?? throw new ValidationException("Badge not found.");

        // One-time (milestone) badges can't be re-awarded; repeatable ones always add a new award.
        if (!badge.Repeatable && await db.ChildBadges.AnyAsync(cb => cb.ChildId == childId && cb.BadgeId == badgeId, ct))
            throw new ValidationException("This child already has that badge, and it can only be awarded once.");

        db.ChildBadges.Add(new ChildBadge { ChildId = childId, BadgeId = badgeId });
        await db.SaveChangesAsync(ct);
    }
}
