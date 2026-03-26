namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Machine-oriented detailed health payload for <c>GET /api/health/detailed</c>.
/// </summary>
public sealed class DetailedHealthReport
{
    /// <summary>Aggregated status: worst case among <see cref="Components"/>.</summary>
    public required string OverallStatus { get; init; }

    /// <summary>UTC instant when the report was built (ISO-8601 when serialized).</summary>
    public required DateTime CheckedAtUtc { get; init; }

    /// <summary>Per-logical-component entries (mock or probe-derived).</summary>
    public required IReadOnlyList<ComponentHealthEntry> Components { get; init; }
}

/// <summary>
/// A single logical component line in <see cref="DetailedHealthReport"/>.
/// </summary>
public sealed class ComponentHealthEntry
{
    public required string Name { get; init; }

    /// <summary>One of <see cref="ComponentHealthStatus"/> values.</summary>
    public required string Status { get; init; }

    /// <summary>Optional probe detail from <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.HealthReportEntry.Description"/>.</summary>
    public string? Description { get; init; }

    /// <summary>Optional execution duration for the check (ISO-8601 duration when serialized).</summary>
    public TimeSpan? Duration { get; init; }
}

/// <summary>
/// Allowed status strings for JSON contract stability.
/// </summary>
public static class ComponentHealthStatus
{
    public const string Healthy = nameof(Healthy);
    public const string Degraded = nameof(Degraded);
    public const string Unhealthy = nameof(Unhealthy);
}
