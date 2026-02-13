using System.Text.Json.Serialization;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Represents a structured log entry for telemetry file output.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets or sets the timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the log level (e.g., Information, Warning, Error).
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category name of the logger.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the exception details, if any.
    /// </summary>
    public LogEntryError? Exception { get; set; }
}
