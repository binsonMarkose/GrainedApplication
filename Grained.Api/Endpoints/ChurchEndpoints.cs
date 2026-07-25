using Grained.Application.Churches;
using Grained.Application.Common.Interfaces;
using Grained.Application.Onboarding;
using Grained.Domain.Enums;

namespace Grained.Api.Endpoints;

public record CreateChurchRequest(string Name, string AdminEmail);

// Churches are managed by SuperAdmin only and are not church-scoped.
public static class ChurchEndpoints
{
    public static void MapChurchEndpoints(this IEndpointRouteBuilder app)
    {
        // Self-service: a ChurchAdmin views/edits their OWN church's details (name/address/email/phone).
        var mine = app.MapGroup("/api/churches/mine").RequireAuthorization("ChurchAdmin").WithTags("Churches");

        mine.MapGet("", async (ICurrentUserService u, IChurchService s, CancellationToken ct) =>
            await s.GetByIdAsync(u.RequireChurchId(), ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        mine.MapPut("", async (ChurchFormModel model, ICurrentUserService u, IChurchService s, CancellationToken ct) =>
        {
            model.Id = u.RequireChurchId();
            await s.UpdateAsync(model, ct);
            return Results.NoContent();
        });

        var group = app.MapGroup("/api/churches").RequireAuthorization("SuperAdmin").WithTags("Churches");

        group.MapGet("", (IChurchService s, CancellationToken ct, bool includeInactive = false, string? status = null) =>
        {
            ChurchStatus? parsed = Enum.TryParse<ChurchStatus>(status, ignoreCase: true, out var st) ? st : null;
            return s.GetAllAsync(includeInactive, parsed, ct);
        });

        group.MapGet("/{id:guid}", async (Guid id, IChurchService s, CancellationToken ct) =>
            await s.GetByIdAsync(id, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        // Provision a church with just a name + admin email → Pending church + emailed invite.
        group.MapPost("", async (
            CreateChurchRequest req,
            ICurrentUserService currentUser,
            IChurchOnboardingService onboarding,
            IInviteEmailSender email,
            IConfiguration config,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var created = await onboarding.CreateChurchWithInviteAsync(
                req.Name, req.AdminEmail, currentUser.UserId ?? Guid.Empty, ct);

            var acceptUrl = BuildAcceptUrl(config, created.RawToken);
            await email.SendChurchAdminInviteAsync(created.Email, created.ChurchName, acceptUrl, "Your Grained administrator", ct);

            return Results.Created($"/api/churches/{created.ChurchId}", new
            {
                churchId = created.ChurchId,
                status = "Pending",
                // Dev-only convenience so the SuperAdmin can copy the link before email is configured.
                acceptUrl = env.IsDevelopment() ? acceptUrl : null,
            });
        });

        // Edit an existing church's details (name/address/email/phone).
        group.MapPut("/{id:guid}", async (Guid id, ChurchFormModel model, IChurchService s, CancellationToken ct) =>
        {
            model.Id = id;
            await s.UpdateAsync(model, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/active", async (Guid id, SetActiveRequest req, IChurchService s, CancellationToken ct) =>
        {
            await s.SetActiveAsync(id, req.IsActive, ct);
            return Results.NoContent();
        });

        // Seed the default Nursery lesson library into an existing church (idempotent — no-op if it
        // already has lessons). New churches get it automatically at onboarding.
        group.MapPost("/{id:guid}/seed-lessons", async (Guid id, Grained.Application.Lessons.ILessonService lessons, CancellationToken ct) =>
        {
            var added = await lessons.SeedDefaultLibraryAsync(id, ct);
            return Results.Ok(new { added });
        });

        // Revoke the church's current pending invite and issue a fresh one.
        group.MapPost("/{id:guid}/resend-invite", async (
            Guid id,
            ICurrentUserService currentUser,
            IChurchOnboardingService onboarding,
            IInviteEmailSender email,
            IConfiguration config,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var created = await onboarding.ResendInviteAsync(id, currentUser.UserId ?? Guid.Empty, ct);
            var acceptUrl = BuildAcceptUrl(config, created.RawToken);
            await email.SendChurchAdminInviteAsync(created.Email, created.ChurchName, acceptUrl, "Your Grained administrator", ct);

            return Results.Ok(new { status = "Pending", acceptUrl = env.IsDevelopment() ? acceptUrl : null });
        });
    }

    private static string BuildAcceptUrl(IConfiguration config, string rawToken)
    {
        var baseUrl = (config["App:WebBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        return $"{baseUrl}/accept-invite?token={Uri.EscapeDataString(rawToken)}";
    }
}
