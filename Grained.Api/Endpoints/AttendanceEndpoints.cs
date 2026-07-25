using Grained.Application.AttendanceTracking;
using Grained.Application.Common.Interfaces;

namespace Grained.Api.Endpoints;

public static class AttendanceEndpoints
{
    public static void MapAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/attendance").RequireAuthorization("Staff").WithTags("Attendance");

        group.MapGet("/roster", (
            ICurrentUserService u, IAttendanceService s, Guid classGroupId, DateOnly date, CancellationToken ct) =>
            s.GetRosterAsync(u.RequireChurchId(), classGroupId, date, ct));

        group.MapPost("", async (AttendanceSaveModel model, ICurrentUserService u, IAttendanceService s, CancellationToken ct) =>
        {
            await s.SaveAsync(u.RequireChurchId(), model, ct);
            return Results.NoContent();
        });
    }
}
