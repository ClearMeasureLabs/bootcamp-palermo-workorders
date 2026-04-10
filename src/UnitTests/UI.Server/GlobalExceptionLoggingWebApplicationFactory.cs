using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

/// <summary>
/// UI.Server test host with an in-memory logger sink for global exception logging assertions.
/// </summary>
public sealed class GlobalExceptionLoggingWebApplicationFactory : WebApplicationFactory<UiServerWebApplicationMarker>
{
    public List<StubLogEntry> CapturedLogs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SqlConnectionString", WebApplicationTestingDatabase.SqliteSharedMemoryConnectionString);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SqlConnectionString"] = WebApplicationTestingDatabase.SqliteSharedMemoryConnectionString,
                ["AI_OpenAI_ApiKey"] = "",
                ["AI_OpenAI_Url"] = "",
                ["AI_OpenAI_Model"] = "",
                ["APPLICATIONINSIGHTS_CONNECTION_STRING"] = "",
                ["ApiKeyAuthentication:Enabled"] = "false",
                ["ApiKeyAuthentication:ValidationKey"] = ""
            });
        });
        builder.ConfigureLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Trace);
            logging.AddProvider(new StubListLoggerProvider(CapturedLogs));
        });
    }
}

internal sealed class StubListLoggerProvider : ILoggerProvider
{
    private readonly List<StubLogEntry> _entries;

    public StubListLoggerProvider(List<StubLogEntry> entries) => _entries = entries;

    public ILogger CreateLogger(string categoryName) => new StubLogger(categoryName, _entries);

    public void Dispose()
    {
    }
}

internal sealed class StubLogger : ILogger
{
    private readonly string _categoryName;
    private readonly List<StubLogEntry> _entries;

    public StubLogger(string categoryName, List<StubLogEntry> entries)
    {
        _categoryName = categoryName;
        _entries = entries;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new StubLogEntry
        {
            Category = _categoryName,
            LogLevel = logLevel,
            Exception = exception,
            Message = formatter(state, exception)
        });
    }
}

public sealed class StubLogEntry
{
    public required string Category { get; init; }
    public required LogLevel LogLevel { get; init; }
    public Exception? Exception { get; init; }
    public required string Message { get; init; }
}
