// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace ChurchBulletin.ServiceDefaults;

/// <summary>
/// Represents exception details for structured log entries.
/// </summary>
public class LogEntryError
{
    /// <summary>
    /// Gets the fully qualified type name of the exception.
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Gets the exception message.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the stack trace of the exception.
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Gets the inner exception details, if any.
    /// </summary>
    public LogEntryError? InnerException { get; init; }

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
