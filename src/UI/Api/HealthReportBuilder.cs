namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds the detailed health report. Initial implementation uses static entries;
/// later work can swap in real health-check probe results.
/// </summary>
public static class HealthReportBuilder
{
    /// <summary>
    /// Logical names align with registered checks in <c>UIServiceRegistry</c> where applicable.
    /// </summary>
    public static DetailedHealthReport Build(TimeProvider? timeProvider = null)
    {
        var clock = timeProvider ?? TimeProvider.System;
        var checkedAt = clock.GetUtcNow().UtcDateTime;

        var components = new ComponentHealthEntry[]
        {
            new() { Name = "LlmGateway", Status = ComponentHealthStatus.Healthy },
            new() { Name = "DataAccess", Status = ComponentHealthStatus.Healthy },
            new() { Name = "Server", Status = ComponentHealthStatus.Healthy },
            new() { Name = "API", Status = ComponentHealthStatus.Healthy },
            new() { Name = "Jeffrey", Status = ComponentHealthStatus.Degraded }
        };

        return new DetailedHealthReport
        {
            CheckedAtUtc = checkedAt,
            Components = components,
            OverallStatus = AggregateWorst(components)
        };
    }

    internal static string AggregateWorst(IReadOnlyList<ComponentHealthEntry> components)
    {
        var rank = 0;
        foreach (var c in components)
        {
            var r = Rank(c.Status);
            if (r > rank) rank = r;
        }

        return rank switch
        {
            2 => ComponentHealthStatus.Unhealthy,
            1 => ComponentHealthStatus.Degraded,
            _ => ComponentHealthStatus.Healthy
        };
    }

    private static int Rank(string status) => status switch
    {
        ComponentHealthStatus.Unhealthy => 2,
        ComponentHealthStatus.Degraded => 1,
        _ => 0
    };
}
