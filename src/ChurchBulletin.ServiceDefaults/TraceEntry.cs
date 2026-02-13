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
    {
        Timestamp = DateTime.UtcNow;
        Status = status;
        TraceId = activity.TraceId.ToString();
        SpanId = activity.SpanId.ToString();
        ParentSpanId = activity.ParentSpanId != default ? activity.ParentSpanId.ToString() : null;
        Name = activity.DisplayName;
        Source = activity.Source.Name;
        DurationMs = activity.Duration.TotalMilliseconds;
        StatusCode = activity.Status != ActivityStatusCode.Unset ? activity.Status.ToString() : null;
        Error = activity.Status == ActivityStatusCode.Error ? activity.StatusDescription : null;
        Tags = activity.Tags.ToDictionary(t => t.Key, t => t.Value);
    }
}
