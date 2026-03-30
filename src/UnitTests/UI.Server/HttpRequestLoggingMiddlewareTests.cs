using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class HttpRequestLoggingMiddlewareTests
{
    [Test]
    public async Task Should_LogMethodPathStatusAndDuration_When_PipelineCompletes()
    {
        var sink = new LogSink();
        var loggerFactory = LoggerFactory.Create(b => b.AddProvider(new SinkLoggerProvider(sink)));
        var logger = loggerFactory.CreateLogger<HttpRequestLoggingMiddleware>();
        var middleware = new HttpRequestLoggingMiddleware(
            _ =>
            {
                _.Response.StatusCode = StatusCodes.Status418ImATeapot;
                return Task.CompletedTask;
            },
            logger);

        var context = new DefaultHttpContext();
        context.Request.Method = "PATCH";
        context.Request.Path = "/api/sample";

        await middleware.InvokeAsync(context);

        sink.Messages.Count.ShouldBe(1);
        var line = sink.Messages[0];
        line.ShouldContain("PATCH");
        line.ShouldContain("/api/sample");
        line.ShouldContain("418");
        line.ShouldContain("ms");
    }

    private sealed class SinkLoggerProvider : ILoggerProvider
    {
        private readonly LogSink _sink;

        public SinkLoggerProvider(LogSink sink) => _sink = sink;

        public ILogger CreateLogger(string categoryName) => new SinkLogger(_sink);

        public void Dispose()
        {
        }
    }

    private sealed class LogSink
    {
        public List<string> Messages { get; } = new();
    }

    private sealed class SinkLogger : ILogger
    {
        private readonly LogSink _sink;

        public SinkLogger(LogSink sink) => _sink = sink;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (formatter is null)
            {
                return;
            }

            _sink.Messages.Add(formatter(state, exception));
        }
    }
}
