using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Fundraising;

public class CampaignService(IApplicationDbContext db) : ICampaignService
{
    public async Task<List<CampaignListItemDto>> GetForChurchAsync(Guid churchId, bool includeInactive = false, CancellationToken ct = default)
    {
        var query = db.Campaigns.AsNoTracking().Where(c => c.ChurchId == churchId);
        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        return await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CampaignListItemDto(
                c.Id, c.ChurchId, c.Title, c.TargetAmount,
                c.Donations.Where(d => d.Payment != null && d.Payment.Status == PaymentStatus.Paid).Sum(d => d.Amount),
                c.LogoImageId, c.IsPublished, c.IsActive,
                c.Donations.Count(d => d.Payment != null && d.Payment.Status == PaymentStatus.Paid)))
            .ToListAsync(ct);
    }

    public async Task<CampaignDetailDto?> GetDetailAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        return await db.Campaigns.AsNoTracking()
            .Where(c => c.Id == id && c.ChurchId == churchId)
            .Select(c => new CampaignDetailDto(
                c.Id, c.ChurchId, c.Title, c.Description, c.TargetAmount,
                c.Donations.Where(d => d.Payment != null && d.Payment.Status == PaymentStatus.Paid).Sum(d => d.Amount),
                c.LogoImageId, c.IsPublished, c.IsActive))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Guid> CreateAsync(Guid churchId, CampaignFormModel model, CancellationToken ct = default)
    {
        var campaign = new Campaign
        {
            ChurchId = churchId,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            TargetAmount = model.TargetAmount
        };
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync(ct);
        return campaign.Id;
    }

    public async Task UpdateAsync(Guid churchId, CampaignFormModel model, CancellationToken ct = default)
    {
        if (model.Id is null)
            throw new ValidationException("Campaign id is required for update.");

        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == model.Id && c.ChurchId == churchId, ct)
            ?? throw new ValidationException("Campaign not found.");

        campaign.Title = model.Title.Trim();
        campaign.Description = model.Description?.Trim();
        campaign.TargetAmount = model.TargetAmount;
        campaign.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task PublishAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var campaign = await Get(id, churchId, ct);
        campaign.IsPublished = true;
        campaign.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task UnpublishAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var campaign = await Get(id, churchId, ct);
        campaign.IsPublished = false;
        campaign.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(Guid id, Guid churchId, bool isActive, CancellationToken ct = default)
    {
        var campaign = await Get(id, churchId, ct);
        campaign.IsActive = isActive;
        await db.SaveChangesAsync(ct);
    }

    public async Task<Guid> SetLogoAsync(Guid id, Guid churchId, byte[] data, string contentType, CancellationToken ct = default)
    {
        var campaign = await Get(id, churchId, ct);

        // Stored in the DB for now (swap for object storage behind the /api/images/{id} URL later).
        var image = new StoredImage { Data = data, ContentType = contentType };
        db.StoredImages.Add(image);
        campaign.LogoImageId = image.Id;
        campaign.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return image.Id;
    }

    public async Task DeleteAsync(Guid id, Guid churchId, CancellationToken ct = default)
    {
        var campaign = await Get(id, churchId, ct);

        // Donations hold payment history — don't cascade them away. Block the delete and let the admin
        // disable the campaign instead.
        if (await db.Donations.AnyAsync(d => d.CampaignId == id, ct))
            throw new ValidationException("This campaign has donations and can’t be deleted. Disable it instead.");

        // Clean up the uploaded logo image if there is one.
        if (campaign.LogoImageId is Guid imageId)
        {
            var image = await db.StoredImages.FirstOrDefaultAsync(x => x.Id == imageId, ct);
            if (image is not null)
                db.StoredImages.Remove(image);
        }

        db.Campaigns.Remove(campaign);
        await db.SaveChangesAsync(ct);
    }

    private async Task<Campaign> Get(Guid id, Guid churchId, CancellationToken ct) =>
        await db.Campaigns.FirstOrDefaultAsync(c => c.Id == id && c.ChurchId == churchId, ct)
            ?? throw new ValidationException("Campaign not found.");
}
