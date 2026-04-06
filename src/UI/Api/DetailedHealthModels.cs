namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Lightweight JSON payload for <c>GET /api/health</c>.
/// </summary>
public sealed class SimpleHealthResponse
{
    /// <summary>Application-reported status (e.g. <see cref="SimpleHealthStatus.Healthy"/>).</summary>
    public required string Status { get; init; }

    /// <summary>Current instant in UTC (ISO-8601 when serialized).</summary>
    public required DateTime CurrentTimeUtc { get; init; }

    /// <summary>Elapsed time since the host process started.</summary>
    public required TimeSpan Uptime { get; init; }
}

/// <summary>
/// Allowed values for <see cref="SimpleHealthResponse.Status"/>.
/// </summary>
public static class SimpleHealthStatus
{
    public const string Healthy = nameof(Healthy);
}

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
    /// <summary>Logical name of the health check component.</summary>
    public required string Name { get; init; }

    /// <summary>One of <see cref="ComponentHealthStatus"/> values.</summary>
    public required string Status { get; init; }

    /// <summary>Human-readable description returned by the health check (null when healthy with no description).</summary>
    public string? Description { get; init; }

    /// <summary>Exception message when the check captured a failure (null when no exception occurred).</summary>
    public string? ExceptionMessage { get; init; }

    /// <summary>Full exception detail including stack trace (null when no exception occurred).</summary>
    public string? ExceptionDetail { get; init; }

    /// <summary>Duration in milliseconds the health check took to execute.</summary>
    public double? DurationMs { get; init; }

    /// <summary>Arbitrary key-value data reported by the health check for additional diagnostic context.</summary>
    public IReadOnlyDictionary<string, object>? Data { get; init; }
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
