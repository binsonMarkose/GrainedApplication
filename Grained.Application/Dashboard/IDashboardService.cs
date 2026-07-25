namespace Grained.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid churchId, CancellationToken ct = default);
}
