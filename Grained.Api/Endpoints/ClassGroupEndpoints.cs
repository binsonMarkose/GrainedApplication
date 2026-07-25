using Grained.Application.ClassGroups;
using Grained.Application.Common.Interfaces;

namespace Grained.Api.Endpoints;

public static class ClassGroupEndpoints
{
    public static void MapClassGroupEndpoints(this IEndpointRouteBuilder app)
    {
        // Staff can read (children/lesson forms need the list); only ChurchAdmin can write.
        var group = app.MapGroup("/api/class-groups").RequireAuthorization("Staff").WithTags("ClassGroups");

        group.MapGet("", (ICurrentUserService u, IClassGroupService s, CancellationToken ct, bool includeInactive = false) =>
            s.GetAllForChurchAsync(u.RequireChurchId(), includeInactive, ct));

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUserService u, IClassGroupService s, CancellationToken ct) =>
            await s.GetByIdAsync(id, u.RequireChurchId(), ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        group.MapPost("", async (ClassGroupFormModel model, ICurrentUserService u, IClassGroupService s, CancellationToken ct) =>
        {
            var id = await s.CreateAsync(u.RequireChurchId(), model, ct);
            return Results.Created($"/api/class-groups/{id}", new { id });
        }).RequireAuthorization("ChurchAdmin");

        group.MapPut("/{id:guid}", async (Guid id, ClassGroupFormModel model, ICurrentUserService u, IClassGroupService s, CancellationToken ct) =>
        {
            model.Id = id;
            await s.UpdateAsync(u.RequireChurchId(), model, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/active", async (Guid id, SetActiveRequest req, ICurrentUserService u, IClassGroupService s, CancellationToken ct) =>
        {
            await s.SetActiveAsync(id, u.RequireChurchId(), req.IsActive, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapDelete("/{id:guid}", async (Guid id, ICurrentUserService u, IClassGroupService s, CancellationToken ct) =>
        {
            await s.DeleteAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");
    }
}
