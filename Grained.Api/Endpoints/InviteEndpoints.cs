using Grained.Api.Auth;
using Grained.Application.Onboarding;
using Grained.Domain.Common;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Grained.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Grained.Api.Endpoints;

public record AcceptInviteRequest(
    string Token, string FirstName, string LastName, string? Address, string? Phone, string Password);

public static class InviteEndpoints
{
    public static void MapInviteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invites").WithTags("Invites").RequireRateLimiting("invite");

        // Public: validate a token so the accept page can render the church name + email.
        group.MapGet("", async (string token, IChurchOnboardingService onboarding, CancellationToken ct) =>
        {
            var info = await onboarding.GetInviteInfoAsync(token, ct);
            return info is null
                ? Results.Json(new { message = "This invite link is invalid or has expired." }, statusCode: StatusCodes.Status410Gone)
                : Results.Ok(info);
        }).AllowAnonymous();

        // Public: accept the invite — create the ChurchAdmin, activate the church, sign them in.
        // Everything happens in one transaction so a partial failure rolls back completely.
        group.MapPost("/accept", async (
            AcceptInviteRequest req,
            IInviteTokenService tokens,
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            JwtTokenService jwt,
            CancellationToken ct) =>
        {
            const string genericError = "This invite can't be completed. Ask your administrator to resend it.";

            if (!tokens.TryValidate(req.Token, out var invitationId))
                return Results.Json(new { message = genericError }, statusCode: StatusCodes.Status410Gone);

            var invite = await db.Invitations.Include(i => i.Church).FirstOrDefaultAsync(i => i.Id == invitationId, ct);
            if (invite is null
                || invite.Status != InvitationStatus.Pending
                || invite.ExpiresAtUtc <= DateTime.UtcNow
                || !string.Equals(invite.TokenHash, tokens.Hash(req.Token), StringComparison.Ordinal))
            {
                return Results.Json(new { message = genericError }, statusCode: StatusCodes.Status410Gone);
            }

            // Multi-church membership is a later ticket; for now one admin ↔ one church.
            if (await userManager.FindByEmailAsync(invite.Email) is not null)
                return Results.Json(new { message = genericError }, statusCode: StatusCodes.Status409Conflict);

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var user = new ApplicationUser
            {
                UserName = invite.Email,
                Email = invite.Email,
                EmailConfirmed = true, // the token proves control of the inbox
                FullName = $"{req.FirstName.Trim()} {req.LastName.Trim()}".Trim(),
                ChurchId = invite.ChurchId,
                IsActive = true,
            };

            var created = await userManager.CreateAsync(user, req.Password);
            if (!created.Succeeded)
                return Results.BadRequest(new { message = string.Join(" ", created.Errors.Select(e => e.Description)) });

            await userManager.AddToRoleAsync(user, Roles.ChurchAdmin);

            invite.Status = InvitationStatus.Accepted;
            invite.AcceptedAtUtc = DateTime.UtcNow;

            invite.Church.Status = ChurchStatus.Active;
            invite.Church.ActivatedAtUtc = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(req.Address)) invite.Church.Address = req.Address.Trim();
            if (!string.IsNullOrWhiteSpace(req.Phone)) invite.Church.Phone = req.Phone.Trim();

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var roles = await userManager.GetRolesAsync(user);
            var (jwtToken, expires) = jwt.Create(user, roles);
            var dto = new UserDto(user.Id, user.Email ?? "", user.FullName, user.ChurchId, roles.ToList(), user.MustChangePassword);
            return Results.Ok(new LoginResponse(jwtToken, expires, dto));
        }).AllowAnonymous();
    }
}
