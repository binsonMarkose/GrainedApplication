using System.Text.RegularExpressions;
using Grained.Application.Badges;
using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Application.Lessons;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Onboarding;

// Data-side of onboarding (church + invitation rows + token). Actually creating the Identity user
// and activating the church happens in the API accept endpoint (it needs UserManager + a
// transaction). Email is sent from the API too (it builds the accept URL from config).
public partial class ChurchOnboardingService(IApplicationDbContext db, IInviteTokenService tokens)
    : IChurchOnboardingService
{
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

    public async Task<CreatedInvite> CreateChurchWithInviteAsync(
        string churchName, string adminEmail, Guid createdByUserId, CancellationToken ct = default)
    {
        churchName = churchName?.Trim() ?? "";
        adminEmail = NormalizeEmail(adminEmail);

        if (string.IsNullOrWhiteSpace(churchName))
            throw new ValidationException("Church name is required.");
        if (!IsValidEmail(adminEmail))
            throw new ValidationException("A valid admin email is required.");

        var church = new Church
        {
            Name = churchName,
            Email = adminEmail,
            Status = ChurchStatus.Pending,
            Slug = await UniqueSlugAsync(churchName, ct),
            CreatedByUserId = createdByUserId,
            IsActive = true,
        };
        db.Churches.Add(church);

        // Provision the starter badge set + Nursery lesson library so the church has content on day one.
        db.Badges.AddRange(DefaultBadges.ForChurch(church.Id));
        db.Lessons.AddRange(DefaultLessons.ForChurch(church.Id));

        var invite = await IssueInvitationAsync(church.Id, adminEmail, createdByUserId, ct);

        await db.SaveChangesAsync(ct);
        return new CreatedInvite(church.Id, invite.Id, adminEmail, churchName, invite.RawToken);
    }

    public async Task<InviteInfo?> GetInviteInfoAsync(string token, CancellationToken ct = default)
    {
        if (!tokens.TryValidate(token, out var invitationId))
            return null;

        var invite = await db.Invitations.AsNoTracking()
            .Include(i => i.Church)
            .FirstOrDefaultAsync(i => i.Id == invitationId, ct);

        if (invite is null || invite.Status != InvitationStatus.Pending)
            return null;
        if (invite.ExpiresAtUtc <= DateTime.UtcNow)
            return null;
        if (!string.Equals(invite.TokenHash, tokens.Hash(token), StringComparison.Ordinal))
            return null;

        return new InviteInfo(invite.Church.Name, invite.Email);
    }

    public async Task<CreatedInvite> ResendInviteAsync(Guid churchId, Guid byUserId, CancellationToken ct = default)
    {
        var church = await db.Churches.FirstOrDefaultAsync(c => c.Id == churchId, ct)
            ?? throw new ValidationException("Church not found.");
        if (church.Status != ChurchStatus.Pending)
            throw new ValidationException("Only pending churches can have their invite resent.");

        // Revoke any outstanding pending invites for this church.
        var pending = await db.Invitations
            .Where(i => i.ChurchId == churchId && i.Status == InvitationStatus.Pending)
            .ToListAsync(ct);
        foreach (var p in pending)
            p.Status = InvitationStatus.Revoked;

        var invite = await IssueInvitationAsync(church.Id, church.Email, byUserId, ct);
        await db.SaveChangesAsync(ct);
        return new CreatedInvite(church.Id, invite.Id, church.Email, church.Name, invite.RawToken);
    }

    // --- helpers ---

    private async Task<(Guid Id, string RawToken)> IssueInvitationAsync(
        Guid churchId, string email, Guid createdByUserId, CancellationToken ct)
    {
        var invitation = new Invitation
        {
            ChurchId = churchId,
            Email = email,
            Role = MembershipRole.ChurchAdmin,
            Status = InvitationStatus.Pending,
            ExpiresAtUtc = DateTime.UtcNow.Add(InviteLifetime),
            CreatedByUserId = createdByUserId,
        };
        var raw = tokens.CreateToken(invitation.Id, InviteLifetime);
        invitation.TokenHash = tokens.Hash(raw);
        db.Invitations.Add(invitation);
        await Task.CompletedTask;
        return (invitation.Id, raw);
    }

    private async Task<string> UniqueSlugAsync(string name, CancellationToken ct)
    {
        var baseSlug = Slugify(name);
        if (baseSlug.Length == 0) baseSlug = "church";
        var slug = baseSlug;
        var n = 2;
        while (await db.Churches.AnyAsync(c => c.Slug == slug, ct))
            slug = $"{baseSlug}-{n++}";
        return slug;
    }

    private static string Slugify(string value)
    {
        value = value.ToLowerInvariant();
        value = NonSlugChars().Replace(value, "-");
        return value.Trim('-');
    }

    private static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && EmailPattern().IsMatch(email);

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugChars();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
