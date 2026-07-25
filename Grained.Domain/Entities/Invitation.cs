using Grained.Domain.Enums;

namespace Grained.Domain.Entities;

// A single-use, expiring invite to set up a church account. Only a HASH of the token is stored;
// the raw token lives only in the emailed link.
public class Invitation
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ChurchId { get; set; }
    public Church Church { get; set; } = null!;

    public string Email { get; set; } = string.Empty; // lower-cased
    public MembershipRole Role { get; set; } = MembershipRole.ChurchAdmin;

    public string TokenHash { get; set; } = string.Empty; // SHA-256 of the raw token
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime ExpiresAtUtc { get; set; }

    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? AcceptedAtUtc { get; set; }
}
