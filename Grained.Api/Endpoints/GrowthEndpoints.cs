using Grained.Application.Common.Interfaces;
using Grained.Application.Growth;

namespace Grained.Api.Endpoints;

public static class GrowthEndpoints
{
    public static void MapGrowthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/growth").RequireAuthorization("Staff").WithTags("Growth");

        group.MapGet("/seasons", (ICurrentUserService u, IGrowthService s, CancellationToken ct) =>
            s.ListSeasonsAsync(u.RequireChurchId(), ct));

        group.MapGet("/children", (ICurrentUserService u, IGrowthService s, CancellationToken ct) =>
            s.GetChildStagesAsync(u.RequireChurchId(), ct));

        group.MapPost("/seasons", async (GrowthSeasonFormModel model, ICurrentUserService u, IGrowthService s, CancellationToken ct) =>
        {
            var season = await s.CreateSeasonAsync(u.RequireChurchId(), model, ct);
            return Results.Ok(season);
        }).RequireAuthorization("ChurchAdmin");

        group.MapPut("/seasons/{id:guid}", async (Guid id, GrowthSeasonFormModel model, ICurrentUserService u, IGrowthService s, CancellationToken ct) =>
        {
            var season = await s.UpdateSeasonAsync(id, u.RequireChurchId(), model, ct);
            return Results.Ok(season);
        }).RequireAuthorization("ChurchAdmin");
    }
}
