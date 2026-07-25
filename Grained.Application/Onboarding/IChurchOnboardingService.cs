namespace Grained.Application.Onboarding;

public interface IChurchOnboardingService
{
    // SuperAdmin provisions a church with just a name + the intended admin's email.
    Task<CreatedInvite> CreateChurchWithInviteAsync(
        string churchName, string adminEmail, Guid createdByUserId, CancellationToken ct = default);

    // Public: validate a token without consuming it, for the accept page to render.
    Task<InviteInfo?> GetInviteInfoAsync(string token, CancellationToken ct = default);

    // SuperAdmin: revoke the church's current pending invite and issue a fresh one.
    Task<CreatedInvite> ResendInviteAsync(Guid churchId, Guid byUserId, CancellationToken ct = default);
}
