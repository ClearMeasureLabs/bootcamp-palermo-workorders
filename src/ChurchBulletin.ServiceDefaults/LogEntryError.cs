using System.Text.Json.Serialization;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Represents exception details for structured log entries.
/// </summary>
public class LogEntryError
{
    /// <summary>
    /// Gets or sets the fully qualified type name of the exception.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the exception message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the stack trace of the exception.
    /// </summary>
    public string? StackTrace { get; set; }

    /// <summary>
    /// Gets or sets the inner exception details, if any.
    /// </summary>
    public LogEntryError? InnerException { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntryError"/> class.
    /// </summary>
    public LogEntryError()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntryError"/> class from an exception.
    /// </summary>
    /// <param name="exception">The exception to create the error from. Can be null.</param>
    public LogEntryError(Exception? exception)
    {
        if (exception == null) return;

        Type = exception.GetType().FullName;
        Message = exception.Message;
        StackTrace = exception.StackTrace;
        InnerException = exception.InnerException != null ? new LogEntryError(exception.InnerException) : null;
    }
}
