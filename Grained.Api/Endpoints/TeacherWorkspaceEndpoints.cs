using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Application.TeacherWorkspace;

namespace Grained.Api.Endpoints;

public record MarkLessonCompleteRequest(Guid ClassGroupId, DateOnly Date, List<Guid>? VerseChildIds);
public record AwardBadgeRequest(Guid BadgeId);

public static class TeacherWorkspaceEndpoints
{
    public static void MapTeacherWorkspaceEndpoints(this IEndpointRouteBuilder app)
    {
        // Staff = ChurchAdmin OR Teacher. Writes are additionally scoped to the caller's own
        // assigned classes inside the service (admins are unrestricted).
        var group = app.MapGroup("/api/teacher").RequireAuthorization("Staff").WithTags("TeacherWorkspace");

        group.MapGet("/workspace", (ICurrentUserService u, ITeacherWorkspaceService s, CancellationToken ct) =>
        {
            var userId = u.UserId ?? throw new ValidationException("No user is associated with the current request.");
            return s.GetWorkspaceAsync(userId, u.RequireChurchId(), ct);
        });

        group.MapPost("/lessons/{lessonId:guid}/complete", async (
            Guid lessonId, MarkLessonCompleteRequest req, ICurrentUserService u, ITeacherWorkspaceService s, CancellationToken ct) =>
        {
            var childrenCompleted = await s.MarkLessonCompletedAsync(u.RequireChurchId(), lessonId, req.ClassGroupId, req.Date, req.VerseChildIds ?? [], ct);
            return Results.Ok(new { childrenCompleted });
        });

        group.MapGet("/badges", (ICurrentUserService u, ITeacherWorkspaceService s, CancellationToken ct) =>
            s.GetBadgeCatalogAsync(u.RequireChurchId(), ct));

        group.MapPost("/children/{childId:guid}/badges", async (
            Guid childId, AwardBadgeRequest req, ICurrentUserService u, ITeacherWorkspaceService s, CancellationToken ct) =>
        {
            await s.AwardBadgeAsync(u.RequireChurchId(), childId, req.BadgeId, ct);
            return Results.NoContent();
        });
    }
}
