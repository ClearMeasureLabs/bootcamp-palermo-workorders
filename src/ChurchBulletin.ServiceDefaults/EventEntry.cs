using System.Diagnostics;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Represents a structured event entry for telemetry file output.
/// </summary>
public class EventEntry
{
    /// <summary>
    /// Gets or sets the timestamp when the event occurred.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the trace ID associated with this event.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the span ID associated with this event.
    /// </summary>
    public string SpanId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the event.
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the tags associated with the event.
    /// </summary>
    public Dictionary<string, object?> Tags { get; set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="EventEntry"/> class.
    /// </summary>
    public EventEntry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EventEntry"/> class from an activity event.
    /// </summary>
    /// <param name="activity">The parent activity.</param>
    /// <param name="evt">The activity event to create the entry from.</param>
    public EventEntry(Activity activity, ActivityEvent evt)
    {
        Timestamp = evt.Timestamp.UtcDateTime;
        TraceId = activity.TraceId.ToString();
        SpanId = activity.SpanId.ToString();
        EventName = evt.Name;
        Tags = evt.Tags.ToDictionary(t => t.Key, t => (object?)t.Value);
    }
}
