using Grained.Application.Onboarding;
using Microsoft.Extensions.Logging;

namespace Grained.Infrastructure.Onboarding;

// Dev/default email sender: logs the invite instead of sending it. Swap for a Resend/Brevo/Mailtrap
// implementation (selected by config) without changing the onboarding flow.
public class LoggingInviteEmailSender(ILogger<LoggingInviteEmailSender> logger) : IInviteEmailSender
{
    public Task SendChurchAdminInviteAsync(
        string toEmail, string churchName, string acceptUrl, string invitedByName, CancellationToken ct = default)
    {
        logger.LogInformation(
            "GRAINED INVITE ✉  to={Email}  church=\"{Church}\"  invitedBy={By}\n  Accept: {Url}",
            toEmail, churchName, invitedByName, acceptUrl);
        return Task.CompletedTask;
    }
}
