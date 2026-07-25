using System.Security.Cryptography;
using System.Text;
using Grained.Application.Onboarding;
using Microsoft.AspNetCore.DataProtection;

namespace Grained.Infrastructure.Onboarding;

// Time-limited, tamper-proof tokens via ASP.NET DataProtection (no hand-rolled crypto).
// The token payload is just the invitation id; DataProtection enforces the 7-day lifetime and
// signs it. The DB stores only SHA-256(token) for single-use / revocation checks.
public class InviteTokenService : IInviteTokenService
{
    private readonly ITimeLimitedDataProtector _protector;

    public InviteTokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Grained.Invite.v1").ToTimeLimitedDataProtector();
    }

    public string CreateToken(Guid invitationId, TimeSpan lifetime)
        => _protector.Protect(invitationId.ToString("N"), lifetime);

    public bool TryValidate(string token, out Guid invitationId)
    {
        invitationId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return false;
        try
        {
            var raw = _protector.Unprotect(token);
            return Guid.TryParseExact(raw, "N", out invitationId);
        }
        catch (CryptographicException)
        {
            return false; // tampered or expired
        }
    }

    public string Hash(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
