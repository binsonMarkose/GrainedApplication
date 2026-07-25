using Grained.Application.Common.Interfaces;
using Grained.Application.Teachers;

namespace Grained.Api.Endpoints;

public static class TeacherEndpoints
{
    public static void MapTeacherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teachers").RequireAuthorization("ChurchAdmin").WithTags("Teachers");

        group.MapGet("", (ICurrentUserService u, ITeacherService s, CancellationToken ct, bool includeInactive = false) =>
            s.GetForChurchAsync(u.RequireChurchId(), includeInactive, ct));

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUserService u, ITeacherService s, CancellationToken ct) =>
            await s.GetByIdAsync(id, u.RequireChurchId(), ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        group.MapPost("", async (TeacherFormModel model, ICurrentUserService u, ITeacherService s, CancellationToken ct) =>
        {
            var (teacherProfileId, temporaryPassword) = await s.CreateAsync(u.RequireChurchId(), model, ct);
            return Results.Created($"/api/teachers/{teacherProfileId}", new { teacherProfileId, temporaryPassword });
        });

        group.MapPut("/{id:guid}", async (Guid id, TeacherFormModel model, ICurrentUserService u, ITeacherService s, CancellationToken ct) =>
        {
            model.TeacherProfileId = id;
            await s.UpdateAsync(u.RequireChurchId(), model, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/active", async (Guid id, SetActiveRequest req, ICurrentUserService u, ITeacherService s, CancellationToken ct) =>
        {
            await s.SetActiveAsync(id, u.RequireChurchId(), req.IsActive, ct);
            return Results.NoContent();
        });

        group.MapPost("/{id:guid}/reset-code", async (Guid id, ICurrentUserService u, ITeacherService s, CancellationToken ct) =>
        {
            var temporaryPassword = await s.ResetLoginCodeAsync(id, u.RequireChurchId(), ct);
            return Results.Ok(new { temporaryPassword });
        });

        group.MapDelete("/{id:guid}", async (Guid id, ICurrentUserService u, ITeacherService s, CancellationToken ct) =>
        {
            await s.DeleteAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        });
    }
}
