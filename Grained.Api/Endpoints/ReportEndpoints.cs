using Grained.Application.Common.Interfaces;
using Grained.Application.Reports;

namespace Grained.Api.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports").RequireAuthorization("Staff").WithTags("Reports");

        group.MapGet("/child-progress", (ICurrentUserService u, IReportService s, CancellationToken ct) =>
            s.GetChildProgressReportAsync(u.RequireChurchId(), ct));

        group.MapGet("/child/{id:guid}/badges", (Guid id, ICurrentUserService u, IReportService s, CancellationToken ct) =>
            s.GetChildBadgesAsync(id, u.RequireChurchId(), ct));

        group.MapGet("/class-progress", (ICurrentUserService u, IReportService s, CancellationToken ct) =>
            s.GetClassProgressReportAsync(u.RequireChurchId(), ct));

        group.MapGet("/attendance", (ICurrentUserService u, IReportService s, DateOnly from, DateOnly to, CancellationToken ct) =>
            s.GetAttendanceReportAsync(u.RequireChurchId(), from, to, ct));

        group.MapGet("/lesson-completion", (ICurrentUserService u, IReportService s, CancellationToken ct) =>
            s.GetLessonCompletionReportAsync(u.RequireChurchId(), ct));
    }
}
