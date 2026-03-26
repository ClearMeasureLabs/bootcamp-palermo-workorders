using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="DetailedHealthReport"/> from the ASP.NET Core health pipeline.
/// Entry keys must match logical names registered in <c>UIServiceRegistry.AddHealthChecks</c>
/// (<c>LlmGateway</c>, <c>DataAccess</c>, <c>Server</c>, <c>API</c>, <c>Jeffrey</c>).
/// </summary>
public static class HealthReportBuilder
{
    /// <summary>
    /// Order and names match <c>UIServiceRegistry.AddHealthChecks</c>. Other registrations
    /// (e.g. Aspire <c>self</c>) are omitted so the JSON contract stays stable for operators.
    /// </summary>
    private static readonly string[] UiHealthCheckNames =
    [
        "LlmGateway",
        "DataAccess",
        "Server",
        "API",
        "Jeffrey"
    ];

    /// <summary>
    /// Maps a live <see cref="HealthReport"/> to the JSON-oriented detailed report.
    /// </summary>
    public static DetailedHealthReport FromHealthReport(HealthReport report, TimeProvider? timeProvider = null)
    {
        var clock = timeProvider ?? TimeProvider.System;
        var checkedAt = clock.GetUtcNow().UtcDateTime;

        var components = new List<ComponentHealthEntry>(UiHealthCheckNames.Length);
        foreach (var name in UiHealthCheckNames)
        {
            if (!report.Entries.TryGetValue(name, out var entry))
            {
                components.Add(new ComponentHealthEntry
                {
                    Name = name,
                    Status = ComponentHealthStatus.Unhealthy,
                    Description = "Health check was not present in the report.",
                    Duration = null
                });
                continue;
            }

            components.Add(new ComponentHealthEntry
            {
                Name = name,
                Status = MapStatus(entry.Status),
                Description = entry.Description,
                Duration = entry.Duration
            });
        }

        return new DetailedHealthReport
        {
            CheckedAtUtc = checkedAt,
            Components = components,
            OverallStatus = AggregateWorst(components)
        };
    }

    internal static string MapStatus(HealthStatus status) => status switch
    {
        HealthStatus.Unhealthy => ComponentHealthStatus.Unhealthy,
        HealthStatus.Degraded => ComponentHealthStatus.Degraded,
        _ => ComponentHealthStatus.Healthy
    };

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
