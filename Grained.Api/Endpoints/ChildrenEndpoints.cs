using Grained.Application.Badges;
using Grained.Application.Children;
using Grained.Application.Common.Interfaces;

namespace Grained.Api.Endpoints;

public record AwardBadgeToChildRequest(Guid BadgeId);

public static class ChildrenEndpoints
{
    public static void MapChildrenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/children").RequireAuthorization("Staff").WithTags("Children");

        group.MapGet("", (
            ICurrentUserService u, IChildService s, CancellationToken ct,
            Guid? classGroupId, int? minAge, int? maxAge, bool? isActive) =>
        {
            var filter = new ChildFilter
            {
                ClassGroupId = classGroupId,
                MinAge = minAge,
                MaxAge = maxAge,
                IsActive = isActive ?? true,
            };
            return s.GetForChurchAsync(u.RequireChurchId(), filter, ct);
        });

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUserService u, IChildService s, CancellationToken ct) =>
            await s.GetByIdAsync(id, u.RequireChurchId(), ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        // Warn-before-link: does this parent email already belong to an account? (ChurchAdmin only.)
        group.MapGet("/parent-lookup", (string email, ICurrentUserService u, IChildService s, CancellationToken ct) =>
            s.LookupParentAsync(u.RequireChurchId(), email, ct))
            .RequireAuthorization("ChurchAdmin");

        group.MapPost("", async (ChildFormModel model, ICurrentUserService u, IChildService s, CancellationToken ct) =>
        {
            var id = await s.CreateAsync(u.RequireChurchId(), model, ct);
            return Results.Created($"/api/children/{id}", new { id });
        }).RequireAuthorization("ChurchAdmin");

        group.MapPut("/{id:guid}", async (Guid id, ChildFormModel model, ICurrentUserService u, IChildService s, CancellationToken ct) =>
        {
            model.Id = id;
            await s.UpdateAsync(u.RequireChurchId(), model, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/assign-class", async (Guid id, ClassGroupRef req, ICurrentUserService u, IChildService s, CancellationToken ct) =>
        {
            await s.AssignClassGroupAsync(id, u.RequireChurchId(), req.ClassGroupId, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/active", async (Guid id, SetActiveRequest req, ICurrentUserService u, IChildService s, CancellationToken ct) =>
        {
            await s.SetActiveAsync(id, u.RequireChurchId(), req.IsActive, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/parent-code", async (Guid id, ICurrentUserService u, IChildService s, CancellationToken ct) =>
        {
            var result = await s.CreateOrResetParentCodeAsync(id, u.RequireChurchId(), ct);
            return Results.Ok(new { temporaryPassword = result.Code, linkedExistingAccount = result.LinkedExistingAccount, parentEmail = result.ParentEmail });
        }).RequireAuthorization("ChurchAdmin");

        // ChurchAdmin awards any badge or achievement to a child.
        group.MapPost("/{id:guid}/badges", async (Guid id, AwardBadgeToChildRequest req, ICurrentUserService u, IBadgeService s, CancellationToken ct) =>
        {
            await s.AwardToChildAsync(u.RequireChurchId(), id, req.BadgeId, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapDelete("/{id:guid}", async (Guid id, ICurrentUserService u, IChildService s, CancellationToken ct) =>
        {
            await s.DeleteAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");
    }
}
