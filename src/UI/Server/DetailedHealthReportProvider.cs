using System.Diagnostics;
using ClearMeasure.Bootcamp.UI.Api;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Aggregates <see cref="HealthCheckService"/> results into <see cref="DetailedHealthReport"/>.
/// </summary>
public sealed class DetailedHealthReportProvider(
    HealthCheckService healthCheckService,
    TimeProvider timeProvider) : IDetailedHealthReportProvider
{
    /// <inheritdoc />
    public async Task<DetailedHealthReport> GetReportAsync(CancellationToken cancellationToken = default)
    {
        var report = await healthCheckService.CheckHealthAsync(
            registration => !registration.Tags.Contains("live"),
            cancellationToken);

        return FromHealthReport(report, timeProvider);
    }

    internal static DetailedHealthReport FromHealthReport(
        HealthReport report,
        TimeProvider clock,
        DateTimeOffset? processStartUtc = null)
    {
        var components = report.Entries
            .Select(pair => BuildComponentEntry(pair.Key, pair.Value))
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .ToList();

        return new DetailedHealthReport
        {
            CheckedAtUtc = clock.GetUtcNow().UtcDateTime,
            Components = components,
            OverallStatus = MapOverallStatus(report.Status),
            UptimeSeconds = CalculateUptimeSeconds(clock, processStartUtc)
        };
    }

    internal static DetailedHealthReport FromComponentStatuses(
        IEnumerable<KeyValuePair<string, HealthStatus>> entries,
        HealthStatus aggregateStatus,
        TimeProvider clock,
        DateTimeOffset? processStartUtc = null)
    {
        var components = entries
            .Select(pair => new ComponentHealthEntry
            {
                Name = pair.Key,
                Status = MapComponentStatus(pair.Value)
            })
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .ToList();

        return new DetailedHealthReport
        {
            CheckedAtUtc = clock.GetUtcNow().UtcDateTime,
            Components = components,
            OverallStatus = MapOverallStatus(aggregateStatus),
            UptimeSeconds = CalculateUptimeSeconds(clock, processStartUtc)
        };
    }

    internal static long CalculateUptimeSeconds(TimeProvider clock, DateTimeOffset? processStartUtc = null)
    {
        var startUtc = processStartUtc
            ?? new DateTimeOffset(Process.GetCurrentProcess().StartTime).ToUniversalTime();
        var elapsed = clock.GetUtcNow() - startUtc.ToUniversalTime();
        return (long)elapsed.TotalSeconds;
    }

    private static string MapOverallStatus(HealthStatus status) => status switch
    {
        HealthStatus.Unhealthy => ComponentHealthStatus.Unhealthy,
        HealthStatus.Degraded => ComponentHealthStatus.Degraded,
        _ => ComponentHealthStatus.Healthy
    };

    private static string MapComponentStatus(HealthStatus status) => status switch
    {
        HealthStatus.Unhealthy => ComponentHealthStatus.Unhealthy,
        HealthStatus.Degraded => ComponentHealthStatus.Degraded,
        _ => ComponentHealthStatus.Healthy
    };

    internal static ComponentHealthEntry BuildComponentEntry(string name, HealthReportEntry entry)
    {
        return new ComponentHealthEntry
        {
            Name = name,
            Status = MapComponentStatus(entry.Status),
            Description = entry.Description,
            ExceptionMessage = entry.Exception?.Message,
            ExceptionDetail = entry.Exception?.ToString(),
            DurationMs = entry.Duration.TotalMilliseconds,
            Data = entry.Data.Count > 0
                ? entry.Data.ToDictionary(d => d.Key, d => d.Value)
                : null
        };
    }
}
