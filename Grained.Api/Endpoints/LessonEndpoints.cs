using System.Security.Claims;
using Grained.Application.Common.Interfaces;
using Grained.Application.Lessons;
using Grained.Domain.Enums;
using Grained.Infrastructure.Identity;

namespace Grained.Api.Endpoints;

public static class LessonEndpoints
{
    public static void MapLessonEndpoints(this IEndpointRouteBuilder app)
    {
        // Staff (teachers + admins) can read and author lessons. Teachers are limited to their own
        // lessons (enforced in LessonService); publishing/reviewing is ChurchAdmin-only.
        var group = app.MapGroup("/api/lessons").RequireAuthorization("Staff").WithTags("Lessons");

        group.MapGet("", (ICurrentUserService u, ILessonService s, bool? publishedOnly, LessonStatus? status, Guid? classGroupId, CancellationToken ct) =>
            s.GetForChurchAsync(u.RequireChurchId(), publishedOnly, status, classGroupId, ct));

        // Reorder a group's curriculum. Staff: admins reorder any group; teachers only their own
        // assigned groups (enforced in LessonService). Body: the lesson ids in teaching order.
        group.MapPut("/order", async (ReorderLessonsRequest req, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
        {
            await s.ReorderLessonsAsync(req.ClassGroupId, u.RequireChurchId(), req.LessonIds, ct);
            return Results.NoContent();
        });

        group.MapGet("/{id:guid}", async (Guid id, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
            await s.GetDetailAsync(id, u.RequireChurchId(), ct) is { } dto ? Results.Ok(dto) : Results.NotFound());

        // Author = the current user (teacher or admin). New lessons start as Draft.
        group.MapPost("", async (LessonFormModel model, ClaimsPrincipal principal, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
        {
            var authorName = principal.FindFirstValue(ApplicationUserClaimsPrincipalFactory.FullNameClaimType) ?? "Unknown";
            var id = await s.CreateAsync(u.RequireChurchId(), u.UserId ?? Guid.Empty, authorName, model, ct);
            return Results.Created($"/api/lessons/{id}", new { id });
        });

        group.MapPut("/{id:guid}", async (Guid id, LessonFormModel model, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
        {
            model.Id = id;
            await s.UpdateAsync(u.RequireChurchId(), model, ct);
            return Results.NoContent();
        });

        // Teacher submits their draft for admin review.
        group.MapPost("/{id:guid}/submit", async (Guid id, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
        {
            await s.SubmitForReviewAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        });

        // Admin sends a submitted lesson back to the author with a note.
        group.MapPost("/{id:guid}/send-back", async (Guid id, SendBackRequest req, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
        {
            await s.SendBackAsync(id, u.RequireChurchId(), req.Note, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/publish", async (Guid id, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
        {
            await s.PublishAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/unpublish", async (Guid id, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
        {
            await s.UnpublishAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        // Delete only when unused (guarded in LessonService). ChurchAdmin-only, like publish.
        group.MapDelete("/{id:guid}", async (Guid id, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
        {
            await s.DeleteAsync(id, u.RequireChurchId(), ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/assign-class", async (Guid id, ClassGroupRef req, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
        {
            await s.AssignToClassGroupAsync(id, u.RequireChurchId(), req.ClassGroupId, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/unassign-class", async (Guid id, ClassGroupRef req, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
        {
            await s.UnassignFromClassGroupAsync(id, u.RequireChurchId(), req.ClassGroupId, ct);
            return Results.NoContent();
        }).RequireAuthorization("ChurchAdmin");

        group.MapPost("/{id:guid}/questions", async (Guid id, QuizQuestionFormModel model, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
        {
            var questionId = await s.AddOrUpdateQuestionAsync(id, u.RequireChurchId(), model, ct);
            return Results.Ok(new { questionId });
        });

        group.MapDelete("/{id:guid}/questions/{questionId:guid}", async (Guid id, Guid questionId, ICurrentUserService u, ILessonService s, CancellationToken ct) =>
        {
            await s.RemoveQuestionAsync(id, u.RequireChurchId(), questionId, ct);
            return Results.NoContent();
        });
    }
}
