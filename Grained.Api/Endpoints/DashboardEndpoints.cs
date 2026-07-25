using Grained.Application.Common.Interfaces;
using Grained.Application.Dashboard;

namespace Grained.Api.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/dashboard", async (
            ICurrentUserService currentUser,
            IDashboardService dashboard,
            CancellationToken ct) =>
        {
            // ChurchAdmin / Teacher are scoped to their church. (SuperAdmin church-picking
            // comes with the churches screen; for now they have no single church context.)
            if (currentUser.ChurchId is not { } churchId)
                return Results.BadRequest(new { message = "No church context for the current user." });

            var summary = await dashboard.GetSummaryAsync(churchId, ct);
            return Results.Ok(summary);
        })
        .RequireAuthorization()
        .WithTags("Dashboard");
    }
}
