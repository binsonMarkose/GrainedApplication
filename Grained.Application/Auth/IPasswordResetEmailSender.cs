namespace Grained.Application.Auth;

// Swappable email provider for password-reset links (dev logs it; prod plugs in Resend/Brevo/etc).
public interface IPasswordResetEmailSender
{
    Task SendPasswordResetAsync(string toEmail, string resetUrl, CancellationToken ct = default);
}
