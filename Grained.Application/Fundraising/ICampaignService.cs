namespace Grained.Application.Fundraising;

public interface ICampaignService
{
    Task<List<CampaignListItemDto>> GetForChurchAsync(Guid churchId, bool includeInactive = false, CancellationToken ct = default);
    Task<CampaignDetailDto?> GetDetailAsync(Guid id, Guid churchId, CancellationToken ct = default);
    Task<Guid> CreateAsync(Guid churchId, CampaignFormModel model, CancellationToken ct = default);
    Task UpdateAsync(Guid churchId, CampaignFormModel model, CancellationToken ct = default);
    Task PublishAsync(Guid id, Guid churchId, CancellationToken ct = default);
    Task UnpublishAsync(Guid id, Guid churchId, CancellationToken ct = default);
    Task SetActiveAsync(Guid id, Guid churchId, bool isActive, CancellationToken ct = default);
    Task<Guid> SetLogoAsync(Guid id, Guid churchId, byte[] data, string contentType, CancellationToken ct = default);

    // Permanently removes a campaign (and its logo image). Throws if it has donations — disable it
    // instead in that case.
    Task DeleteAsync(Guid id, Guid churchId, CancellationToken ct = default);
}
