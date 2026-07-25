using System.Security.Claims;
using Grained.Application.Announcements;
using Grained.Application.Common.Interfaces;
using Grained.Infrastructure.Identity;

namespace Grained.Api.Endpoints;

public static class AnnouncementEndpoints
{
    public static void MapAnnouncementEndpoints(this IEndpointRouteBuilder app)
    {
        // --- Author side (ChurchAdmin writes and manages announcements) ---
        var admin = app.MapGroup("/api/announcements").RequireAuthorization("ChurchAdmin").WithTags("Announcements");

        admin.MapGet("", (ICurrentUserService u, IAnnouncementService s, CancellationToken ct) =>
            s.GetForChurchAsync(u.RequireChurchId(), ct));

        admin.MapPost("", async (AnnouncementFormModel model, ClaimsPrincipal principal, ICurrentUserService u, IAnnouncementService s, CancellationToken ct) =>
        {
            var authorName = principal.FindFirstValue(ApplicationUserClaimsPrincipalFactory.FullNameClaimType) ?? "Church admin";
            var id = await s.CreateAsync(u.RequireChurchId(), u.UserId ?? Guid.Empty, authorName, model, ct);
            return Results.Created($"/api/announcements/{id}", new { id });
        });

        admin.MapPost("/{id:guid}/active", async (Guid id, SetActiveRequest req, ICurrentUserService u, IAnnouncementService s, CancellationToken ct) =>
        {
            await s.SetActiveAsync(id, u.RequireChurchId(), req.IsActive, ct);
            return Results.NoContent();
        });

        // --- Recipient side (teachers / parents read their own inbox) ---
        var mine = app.MapGroup("/api/my/announcements").RequireAuthorization().WithTags("Announcements");

        mine.MapGet("", async (ICurrentUserService u, IAnnouncementService s, CancellationToken ct) =>
        {
            // SuperAdmins (no church) and any account without a church have nothing to receive.
            if (u.ChurchId is not { } churchId || u.UserId is not { } userId)
                return Results.Ok(new List<InboxAnnouncementDto>());
            return Results.Ok(await s.GetInboxAsync(churchId, userId, u.IsTeacher, u.IsParent, ct));
        });

        mine.MapPost("/{id:guid}/read", async (Guid id, ICurrentUserService u, IAnnouncementService s, CancellationToken ct) =>
        {
            if (u.UserId is not { } userId) return Results.Unauthorized();
            await s.MarkReadAsync(id, u.RequireChurchId(), userId, ct);
            return Results.NoContent();
        });

        mine.MapPost("/read-all", async (ICurrentUserService u, IAnnouncementService s, CancellationToken ct) =>
        {
            if (u.UserId is not { } userId) return Results.Unauthorized();
            await s.MarkAllReadAsync(u.RequireChurchId(), userId, u.IsTeacher, u.IsParent, ct);
            return Results.NoContent();
        });
    }
}
