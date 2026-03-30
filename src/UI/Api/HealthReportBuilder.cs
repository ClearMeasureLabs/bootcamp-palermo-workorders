namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Helpers for building and aggregating <see cref="DetailedHealthReport"/> component lines.
/// </summary>
public static class HealthReportBuilder
{
    /// <summary>
    /// Builds a report from explicit component lines (for tests and deterministic scenarios).
    /// </summary>
    public static DetailedHealthReport FromEntries(
        TimeProvider timeProvider,
        IReadOnlyList<ComponentHealthEntry> components)
    {
        var clock = timeProvider;
        return new DetailedHealthReport
        {
            CheckedAtUtc = clock.GetUtcNow().UtcDateTime,
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
