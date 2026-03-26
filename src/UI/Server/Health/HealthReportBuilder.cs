namespace ClearMeasure.Bootcamp.UI.Server.Health;

/// <summary>
/// Builds a static detailed health report until live probes are wired.
/// </summary>
public static class HealthReportBuilder
{
    /// <summary>
    /// Returns mock component entries and an <see cref="DetailedHealthReportResponse.OverallStatus"/>
    /// reflecting the worst status among components.
    /// </summary>
    public static DetailedHealthReportResponse Build()
    {
        var components = (IReadOnlyList<ComponentHealthEntry>)
        [
            new ComponentHealthEntry("ApiLayer", "Healthy"),
            new ComponentHealthEntry("CacheConnectivity", "Healthy"),
            new ComponentHealthEntry("ExternalServices", "Degraded"),
        ];

        return new DetailedHealthReportResponse
        {
            OverallStatus = WorstOverall(components),
            CheckedAtUtc = DateTime.UtcNow,
            Components = components,
        };
    }

    internal static string WorstOverall(IReadOnlyList<ComponentHealthEntry> components)
    {
        var worst = 0;
        foreach (var c in components)
        {
            var rank = Rank(c.Status);
            if (rank > worst)
            {
                worst = rank;
            }
        }

        return worst switch
        {
            2 => "Unhealthy",
            1 => "Degraded",
            _ => "Healthy",
        };
    }

    private static int Rank(string status) =>
        status switch
        {
            "Unhealthy" => 2,
            "Degraded" => 1,
            "Healthy" => 0,
            _ => 2,
        };
}
