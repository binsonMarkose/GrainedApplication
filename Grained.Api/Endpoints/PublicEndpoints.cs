using Grained.Application.Common.Interfaces;
using Grained.Application.Public;
using Microsoft.EntityFrameworkCore;

namespace Grained.Api.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        // Anonymous, public-facing storefront + registration + donation. Rate-limited; no admin data.
        var group = app.MapGroup("/api/public").AllowAnonymous().RequireRateLimiting("public").WithTags("Public");

        group.MapGet("/churches/{slug}", async (string slug, IPublicEventService s, CancellationToken ct) =>
            await s.GetStorefrontAsync(slug, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        group.MapGet("/events/{id:guid}", async (Guid id, IPublicEventService s, CancellationToken ct) =>
            await s.GetEventAsync(id, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        group.MapPost("/events/{id:guid}/register", async (Guid id, EventRegistrationModel model, IPublicEventService s, CancellationToken ct) =>
        {
            var result = await s.RegisterAsync(id, model, ct);
            return Results.Ok(result);
        });

        group.MapGet("/campaigns/{id:guid}", async (Guid id, IPublicCampaignService s, CancellationToken ct) =>
            await s.GetCampaignAsync(id, ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        group.MapPost("/campaigns/{id:guid}/donate", async (Guid id, DonationModel model, IPublicCampaignService s, CancellationToken ct) =>
        {
            var result = await s.DonateAsync(id, model, ct);
            return Results.Ok(result);
        });

        // Public image serving (campaign logos). Anonymous + cacheable; stored in the DB for now.
        app.MapGet("/api/images/{id:guid}", async (Guid id, IApplicationDbContext db, CancellationToken ct) =>
        {
            var img = await db.StoredImages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            return img is null ? Results.NotFound() : Results.File(img.Data, img.ContentType);
        }).AllowAnonymous().RequireRateLimiting("public").WithTags("Public");
    }
}
