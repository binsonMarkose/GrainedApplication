using Grained.Application.Common.Interfaces;
using Grained.Application.Fundraising;

namespace Grained.Api.Endpoints;

public static class CampaignEndpoints
{
    private static readonly string[] AllowedImageTypes = ["image/png", "image/jpeg", "image/webp", "image/gif", "image/svg+xml"];
    private const long MaxLogoBytes = 2 * 1024 * 1024; // 2 MB

    public static void MapCampaignEndpoints(this IEndpointRouteBuilder app)
    {
        // Staff can read campaigns; only ChurchAdmin can create/edit/publish/upload.
        var group = app.MapGroup("/api/campaigns").RequireAuthorization("Staff").WithTags("Campaigns");

        group.MapGet("", (ICurrentUserService u, ICampaignService s, CancellationToken ct, bool includeInactive = false) =>
            s.GetForChurchAsync(u.RequireChurchId(), includeInactive, ct));

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUserService u, ICampaignService s, CancellationToken ct) =>
            await s.GetDetailAsync(id, u.RequireChurchId(), ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        group.MapPost("", async (CampaignFormModel model, ICurrentUserService u, ICampaignService s, CancellationToken ct) =>
        {
            var id = await s.CreateAsync(u.RequireChurchId(), model, ct);
            return Results.Created($"/api/campaigns/{id}", new { id });
        }).RequireAuthorization("ChurchAdmin");

        group.MapPut("/{id:guid}", async (Guid id, CampaignFormModel model, ICurrentUserService u, ICampaignService s, CancellationToken ct) =>
        {
            model.Id = id;
            await s.UpdateAsync(u.RequireChurchId(), model, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/publish", async (Guid id, ICurrentUserService u, ICampaignService s, CancellationToken ct) =>
        {
            await s.PublishAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/unpublish", async (Guid id, ICurrentUserService u, ICampaignService s, CancellationToken ct) =>
        {
            await s.UnpublishAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/active", async (Guid id, SetActiveRequest req, ICurrentUserService u, ICampaignService s, CancellationToken ct) =>
        {
            await s.SetActiveAsync(id, u.RequireChurchId(), req.IsActive, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        // Logo upload (multipart). Bearer auth, so antiforgery isn't applicable — disable it.
        group.MapPost("/{id:guid}/logo", async (Guid id, IFormFile file, ICurrentUserService u, ICampaignService s, CancellationToken ct) =>
        {
            if (file.Length == 0)
                return Results.BadRequest(new { message = "No file uploaded." });
            if (file.Length > MaxLogoBytes)
                return Results.BadRequest(new { message = "Image must be 2 MB or smaller." });
            if (!AllowedImageTypes.Contains(file.ContentType))
                return Results.BadRequest(new { message = "Use a PNG, JPEG, WebP, GIF or SVG image." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var imageId = await s.SetLogoAsync(id, u.RequireChurchId(), ms.ToArray(), file.ContentType, ct);
            return Results.Ok(new { imageId });
        }).RequireAuthorization("ChurchAdmin").DisableAntiforgery();

        group.MapDelete("/{id:guid}", async (Guid id, ICurrentUserService u, ICampaignService s, CancellationToken ct) =>
        {
            await s.DeleteAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");
    }
}
