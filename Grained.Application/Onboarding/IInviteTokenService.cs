namespace Grained.Application.Onboarding;

// Issues and validates invite tokens. Implemented with ASP.NET DataProtection (time-limited,
// tamper-proof) — no hand-rolled crypto. The DB additionally stores only a HASH of the token.
public interface IInviteTokenService
{
    string CreateToken(Guid invitationId, TimeSpan lifetime);

    // Returns true and the embedded invitation id if the token is well-formed and unexpired.
    bool TryValidate(string token, out Guid invitationId);

    // SHA-256 (hex) of the raw token — this is what gets persisted for single-use / revocation.
    string Hash(string token);
}
