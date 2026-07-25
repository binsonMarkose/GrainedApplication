using Grained.Application.Common.Interfaces;
using Grained.Application.Events;

namespace Grained.Api.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        // Staff can read events; only ChurchAdmin can create/edit/publish them.
        var group = app.MapGroup("/api/events").RequireAuthorization("Staff").WithTags("Events");

        group.MapGet("", (ICurrentUserService u, IEventService s, CancellationToken ct, bool includeInactive = false) =>
            s.GetForChurchAsync(u.RequireChurchId(), includeInactive, ct));

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUserService u, IEventService s, CancellationToken ct) =>
            await s.GetDetailAsync(id, u.RequireChurchId(), ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        group.MapPost("", async (EventFormModel model, ICurrentUserService u, IEventService s, CancellationToken ct) =>
        {
            var id = await s.CreateAsync(u.RequireChurchId(), model, ct);
            return Results.Created($"/api/events/{id}", new { id });
        }).RequireAuthorization("ChurchAdmin");

        group.MapPut("/{id:guid}", async (Guid id, EventFormModel model, ICurrentUserService u, IEventService s, CancellationToken ct) =>
        {
            model.Id = id;
            await s.UpdateAsync(u.RequireChurchId(), model, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/publish", async (Guid id, ICurrentUserService u, IEventService s, CancellationToken ct) =>
        {
            await s.PublishAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/unpublish", async (Guid id, ICurrentUserService u, IEventService s, CancellationToken ct) =>
        {
            await s.UnpublishAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/active", async (Guid id, SetActiveRequest req, ICurrentUserService u, IEventService s, CancellationToken ct) =>
        {
            await s.SetActiveAsync(id, u.RequireChurchId(), req.IsActive, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapDelete("/{id:guid}", async (Guid id, ICurrentUserService u, IEventService s, CancellationToken ct) =>
        {
            await s.DeleteAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");
    }
}
