using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Application.ParentWorkspace;

namespace Grained.Api.Endpoints;

public static class ParentEndpoints
{
    public static void MapParentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/parent").RequireAuthorization("Parent").WithTags("Parent");

        group.MapGet("/children", (ICurrentUserService u, IParentWorkspaceService s, CancellationToken ct) =>
        {
            var userId = u.UserId ?? throw new ValidationException("No user is associated with the current request.");
            return s.GetForParentAsync(userId, u.RequireChurchId(), ct);
        });

        group.MapPost("/children/{childId:guid}/avatar", async (
            Guid childId, SetAvatarRequest req, ICurrentUserService u, IParentWorkspaceService s, CancellationToken ct) =>
        {
            var userId = u.UserId ?? throw new ValidationException("No user is associated with the current request.");
            await s.SetChildAvatarAsync(userId, u.RequireChurchId(), childId, req.AvatarId, ct);
            return Results.NoContent();
        });

        group.MapGet("/lessons/{lessonId:guid}", async (
            Guid lessonId, ICurrentUserService u, IParentWorkspaceService s, CancellationToken ct) =>
        {
            var userId = u.UserId ?? throw new ValidationException("No user is associated with the current request.");
            return Results.Ok(await s.GetLessonAsync(userId, u.RequireChurchId(), lessonId, ct));
        });
    }
}

public record SetAvatarRequest(string? AvatarId);
