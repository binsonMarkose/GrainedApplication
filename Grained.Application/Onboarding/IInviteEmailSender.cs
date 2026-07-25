namespace Grained.Application.Onboarding;

// Swappable email provider for onboarding invites. Dev uses a logging implementation; prod plugs
// in Resend/Brevo/Mailtrap via config without changing the flow.
public interface IInviteEmailSender
{
    Task SendChurchAdminInviteAsync(
        string toEmail,
        string churchName,
        string acceptUrl,
        string invitedByName,
        CancellationToken ct = default);
}
