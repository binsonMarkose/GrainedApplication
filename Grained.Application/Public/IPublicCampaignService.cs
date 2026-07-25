namespace Grained.Application.Public;

public interface IPublicCampaignService
{
    Task<PublicCampaignDto?> GetCampaignAsync(Guid campaignId, CancellationToken ct = default);
    Task<DonationResultDto> DonateAsync(Guid campaignId, DonationModel model, CancellationToken ct = default);
}
