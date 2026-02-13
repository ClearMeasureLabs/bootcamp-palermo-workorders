namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Represents a structured metric entry for telemetry file output.
/// </summary>
public class MetricEntry
{
    /// <summary>
    /// Gets or sets the timestamp when the metric was recorded.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the name of the metric.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the metric.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// Gets or sets the unit of measurement for the metric.
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Gets or sets the tags associated with the metric.
    /// </summary>
    public IDictionary<string, object?> Tags { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricEntry"/> class.
    /// </summary>
    public MetricEntry()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricEntry"/> class with specified values.
    /// </summary>
    /// <param name="name">The name of the metric.</param>
    /// <param name="value">The value of the metric.</param>
    /// <param name="unit">The unit of measurement (optional).</param>
    /// <param name="tags">The tags associated with the metric (optional).</param>
    public MetricEntry(string name, double value, string? unit = null, IDictionary<string, object?>? tags = null)
    {
        Timestamp = DateTime.UtcNow;
        Name = name;
        Value = value;
        Unit = string.IsNullOrEmpty(unit) ? null : unit;
        Tags = tags ?? new Dictionary<string, object?>();
    }
}
