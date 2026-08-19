using System.Diagnostics;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Represents a structured trace entry for telemetry file output.
/// </summary>
public class TraceEntry
{
    /// <summary>
    /// Gets or sets the timestamp when the trace entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the status of the activity (e.g., STARTED, STOPPED).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the trace ID.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the span ID.
    /// </summary>
    public string SpanId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent span ID, if any.
    /// </summary>
    public string? ParentSpanId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the activity.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the source name of the activity.
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration of the activity in milliseconds.
    /// </summary>
    public double DurationMs { get; set; }

    /// <summary>
    /// Gets or sets the status code of the activity.
    /// </summary>
    public string? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the error description if the activity failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets the tags associated with the activity.
    /// </summary>
    public Dictionary<string, string?> Tags { get; set; } = [];

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
