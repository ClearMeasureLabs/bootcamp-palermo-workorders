namespace ClearMeasure.Bootcamp.UI.Server.Health;

/// <summary>
/// JSON payload for GET /api/health/detailed.
/// </summary>
public sealed class DetailedHealthReportResponse
{
    public required string OverallStatus { get; init; }

    public required DateTime CheckedAtUtc { get; init; }

    public required IReadOnlyList<ComponentHealthEntry> Components { get; init; }
}

/// <summary>
/// Per-component health row in a detailed report.
/// </summary>
public sealed class ComponentHealthEntry
{
    public ComponentHealthEntry(string name, string status)
    {
        Name = name;
        Status = status;
    }

    public string Name { get; }

    public string Status { get; }
}
