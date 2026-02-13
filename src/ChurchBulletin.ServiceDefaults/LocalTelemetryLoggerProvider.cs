using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Logger provider that writes log entries to the LocalTelemetryFileWriter.
/// </summary>
public class LocalTelemetryLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly LocalTelemetryFileWriter _fileWriter;
    private IExternalScopeProvider? _scopeProvider;

    public LocalTelemetryLoggerProvider(LocalTelemetryFileWriter fileWriter)
    {
        _fileWriter = fileWriter;
    }

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
    }

    public ILogger CreateLogger(string categoryName) => new LocalTelemetryLogger(_fileWriter, categoryName, _scopeProvider);

    public void Dispose() { }

    private class LocalTelemetryLogger : ILogger
    {
        private readonly LocalTelemetryFileWriter _fileWriter;
        private readonly string _categoryName;
        private readonly IExternalScopeProvider? _scopeProvider;

        public LocalTelemetryLogger(LocalTelemetryFileWriter fileWriter, string categoryName, IExternalScopeProvider? scopeProvider)
        {
            _fileWriter = fileWriter;
            _categoryName = categoryName;
            _scopeProvider = scopeProvider;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return _scopeProvider?.Push(state);
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            _fileWriter.WriteLogEntry(logLevel, _categoryName, message, exception);
        }
    }
}
