using ChurchBulletin.ServiceDefaults;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.ServiceDefaults;

[TestFixture]
public class LocalTelemetryLoggerProviderTests
{
    [Test]
    public async Task CreateLogger_ShouldReturnLogger_AndDisposeIsSafe()
    {
        var directory = CreateTempDirectory();
        await using var writer = new LocalTelemetryFileWriter(new StubConfiguration(directory));
        using var provider = new LocalTelemetryLoggerProvider(writer);

        var logger = provider.CreateLogger("Test.Category");

        logger.ShouldNotBeNull();
    }

    [TestCase(LogLevel.Trace, false)]
    [TestCase(LogLevel.Debug, false)]
    [TestCase(LogLevel.Information, true)]
    [TestCase(LogLevel.Warning, true)]
    [TestCase(LogLevel.Error, true)]
    [TestCase(LogLevel.Critical, true)]
    public async Task IsEnabled_ShouldFilterBelowInformation(LogLevel level, bool expected)
    {
        var directory = CreateTempDirectory();
        await using var writer = new LocalTelemetryFileWriter(new StubConfiguration(directory));
        using var provider = new LocalTelemetryLoggerProvider(writer);
        var logger = provider.CreateLogger("Filter.Category");

        logger.IsEnabled(level).ShouldBe(expected);
    }

    [Test]
    public async Task Log_ShouldSkipBelowInformation_WhenWriterStarted()
    {
        var directory = CreateTempDirectory();
        await using var writer = new LocalTelemetryFileWriter(new StubConfiguration(directory));
        using var provider = new LocalTelemetryLoggerProvider(writer);
        var logger = provider.CreateLogger("Skip.Category");

        using var cts = new CancellationTokenSource();
        var start = writer.StartAsync(cts.Token);
        await WaitForLogFilesAsync(directory);

        logger.Log(LogLevel.Debug, new EventId(1), "debug-should-not-appear", null, static (s, _) => s);

        await cts.CancelAsync();
        await writer.StopAsync(CancellationToken.None);
        await start;

        var content = await ReadCombinedLogContentAsync(directory);
        content.ShouldNotContain("debug-should-not-appear");
    }

    [Test]
    public async Task Log_ShouldWriteCategoryMessageAndException_WhenInformationOrAbove()
    {
        var directory = CreateTempDirectory();
        await using var writer = new LocalTelemetryFileWriter(new StubConfiguration(directory));
        using var provider = new LocalTelemetryLoggerProvider(writer);
        var logger = provider.CreateLogger("Emit.Category");

        using var cts = new CancellationTokenSource();
        var start = writer.StartAsync(cts.Token);
        await WaitForLogFilesAsync(directory);

        logger.Log(
            LogLevel.Error,
            new EventId(42),
            "emit-should-appear",
            new InvalidOperationException("boom-details"),
            static (s, _) => s);

        await cts.CancelAsync();
        await writer.StopAsync(CancellationToken.None);
        await start;

        var content = await ReadCombinedLogContentAsync(directory);
        content.ShouldContain("Emit.Category");
        content.ShouldContain("emit-should-appear");
        content.ShouldContain("boom-details");
    }

    [Test]
    public async Task BeginScope_ShouldPushState_WhenScopeProviderSet()
    {
        var directory = CreateTempDirectory();
        await using var writer = new LocalTelemetryFileWriter(new StubConfiguration(directory));
        using var provider = new LocalTelemetryLoggerProvider(writer);
        var scopeProvider = new StubExternalScopeProvider();
        provider.SetScopeProvider(scopeProvider);

        var logger = provider.CreateLogger("Scope.Category");
        using var scope = logger.BeginScope("scope-state");

        scopeProvider.PushCount.ShouldBe(1);
        scopeProvider.LastState.ShouldBe("scope-state");
        scope.ShouldNotBeNull();
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "telemetry-provider-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task WaitForLogFilesAsync(string directory)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline && Directory.GetFiles(directory, "logs_*.jsonl").Length == 0)
        {
            await Task.Delay(25);
        }

        Directory.GetFiles(directory, "logs_*.jsonl").Length.ShouldBeGreaterThanOrEqualTo(1);
    }

    private static async Task<string> ReadCombinedLogContentAsync(string directory)
    {
        var parts = new List<string>();
        foreach (var file in Directory.GetFiles(directory, "logs_*.jsonl"))
        {
            parts.Add(await File.ReadAllTextAsync(file));
        }

        return string.Join(Environment.NewLine, parts);
    }

    private sealed class StubExternalScopeProvider : IExternalScopeProvider
    {
        public int PushCount { get; private set; }
        public object? LastState { get; private set; }

        public void ForEachScope<TState>(Action<object?, TState> callback, TState state)
        {
        }

        public IDisposable Push(object? state)
        {
            PushCount++;
            LastState = state;
            return EmptyDisposable.Instance;
        }

        private sealed class EmptyDisposable : IDisposable
        {
            public static EmptyDisposable Instance { get; } = new();
            public void Dispose()
            {
            }
        }
    }
}
