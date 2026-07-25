namespace Grained.Application.Onboarding;

// Returned to the API after a church + invite is created (or resent). RawToken is used by the API
// to build the emailed accept link; it is never persisted (only its hash is) and never returned
// to clients outside Development.
public record CreatedInvite(Guid ChurchId, Guid InvitationId, string Email, string ChurchName, string RawToken);

// What the public accept page needs to render before the user submits.
public record InviteInfo(string ChurchName, string Email);
