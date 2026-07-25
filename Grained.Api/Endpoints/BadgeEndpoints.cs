using Grained.Application.Badges;
using Grained.Application.Common.Interfaces;

namespace Grained.Api.Endpoints;

public static class BadgeEndpoints
{
    public static void MapBadgeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/badges").RequireAuthorization("ChurchAdmin").WithTags("Badges");

        group.MapGet("", (ICurrentUserService u, IBadgeService s, CancellationToken ct, bool includeInactive = false) =>
            s.GetForChurchAsync(u.RequireChurchId(), includeInactive, ct));

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUserService u, IBadgeService s, CancellationToken ct) =>
            await s.GetByIdAsync(id, u.RequireChurchId(), ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        group.MapPost("", async (BadgeFormModel model, ICurrentUserService u, IBadgeService s, CancellationToken ct) =>
        {
            var id = await s.CreateAsync(u.RequireChurchId(), model, ct);
            return Results.Created($"/api/badges/{id}", new { id });
        });

        group.MapPut("/{id:guid}", async (Guid id, BadgeFormModel model, ICurrentUserService u, IBadgeService s, CancellationToken ct) =>
        {
            model.Id = id;
            await s.UpdateAsync(u.RequireChurchId(), model, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/active", async (Guid id, SetActiveRequest req, ICurrentUserService u, IBadgeService s, CancellationToken ct) =>
        {
            await s.SetActiveAsync(id, u.RequireChurchId(), req.IsActive, ct);
            return Results.NoContent();
        });
    }
}
