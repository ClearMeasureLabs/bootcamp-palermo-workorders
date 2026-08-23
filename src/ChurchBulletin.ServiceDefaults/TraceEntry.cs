using System.Diagnostics;

namespace ChurchBulletin.ServiceDefaults;

/// <summary>
/// Represents a structured trace entry for telemetry file output.
/// </summary>
public class TraceEntry
{
    /// <summary>
    /// Gets the timestamp when the trace entry was created.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the status of the activity (e.g., STARTED, STOPPED).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets the trace ID.
    /// </summary>
    public string TraceId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the span ID.
    /// </summary>
    public string SpanId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the parent span ID, if any.
    /// </summary>
    public string? ParentSpanId { get; init; }

    /// <summary>
    /// Gets the display name of the activity.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the source name of the activity.
    /// </summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>
    /// Gets the duration of the activity in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }

    /// <summary>
    /// Gets the status code of the activity.
    /// </summary>
    public string? StatusCode { get; init; }

    /// <summary>
    /// Gets the error description if the activity failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets the tags associated with the activity.
    /// </summary>
    public Dictionary<string, string?> Tags { get; init; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceEntry"/> class.
    /// </summary>
    public TraceEntry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TraceEntry"/> class from an activity.
    /// </summary>
    /// <param name="activity">The activity to create the trace entry from.</param>
    /// <param name="status">The status of the activity.</param>
    public TraceEntry(Activity activity, string status)
        : this()
    {
        var mapped = TraceEntryMapper.FromActivity(activity, status);
        Timestamp = mapped.Timestamp;
        Status = mapped.Status;
        TraceId = mapped.TraceId;
        SpanId = mapped.SpanId;
        ParentSpanId = mapped.ParentSpanId;
        Name = mapped.Name;
        Source = mapped.Source;
        DurationMs = mapped.DurationMs;
        StatusCode = mapped.StatusCode;
        Error = mapped.Error;
        Tags = mapped.Tags;
    }
}
