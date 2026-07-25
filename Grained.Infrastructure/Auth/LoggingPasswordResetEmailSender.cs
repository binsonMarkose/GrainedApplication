using Grained.Application.Auth;
using Microsoft.Extensions.Logging;

namespace Grained.Infrastructure.Auth;

// Dev/default: logs the reset link. Swap for a real provider (selected by config) later.
public class LoggingPasswordResetEmailSender(ILogger<LoggingPasswordResetEmailSender> logger)
    : IPasswordResetEmailSender
{
    public Task SendPasswordResetAsync(string toEmail, string resetUrl, CancellationToken ct = default)
    {
        logger.LogInformation("GRAINED PASSWORD RESET ✉  to={Email}\n  Reset: {Url}", toEmail, resetUrl);
        return Task.CompletedTask;
    }
}
