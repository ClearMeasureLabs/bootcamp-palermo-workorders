using System.Diagnostics;
using ClearMeasure.Bootcamp.ServiceDefaults;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.ServiceDefaults;

[TestFixture]
public class LocalTelemetryFileWriterTests
{
    [Test]
    public void ShouldDeleteOldJsonlFiles_WhenCleanupRuns()
    {
        var directory = CreateTempDirectory();
        var oldFile = Path.Combine(directory, "logs_1999-01-01.jsonl");
        File.WriteAllText(oldFile, "{}");
        File.SetCreationTimeUtc(oldFile, DateTime.UtcNow.AddDays(-10));

        TelemetryFileMaintenance.DeleteFilesOlderThan(directory, 7);

        File.Exists(oldFile).ShouldBeFalse();
    }

    [Test]
    public async Task ShouldDisposeWriters_WhenDisposeAsyncCalled()
    {
        await using var writer = new StreamWriter(Path.GetTempFileName());
        await TelemetryFileMaintenance.DisposeWritersAsync(writer);
    }

    [Test]
    public void ShouldUseConfiguredDirectory_WhenConfigurationProvided()
    {
        var directory = CreateTempDirectory();
        var writer = new LocalTelemetryFileWriter(new StubConfiguration(directory));

        writer.TelemetryLogDirectory.ShouldBe(directory);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "telemetry-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

[TestFixture]
public class TraceEntryTests
{
    [Test]
    public void ShouldMapActivityFields_WhenConstructedFromActivity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        using var source = new ActivitySource("TestSource");
        using var activity = source.StartActivity("sample");
        activity.ShouldNotBeNull();
        activity!.SetTag("key", "value");

        var entry = new TraceEntry(activity, "STARTED");

        entry.Status.ShouldBe("STARTED");
        entry.Name.ShouldBe("sample");
        entry.Source.ShouldBe("TestSource");
        entry.TraceId.ShouldBe(activity.TraceId.ToString());
        entry.SpanId.ShouldBe(activity.SpanId.ToString());
        entry.Tags["key"].ShouldBe("value");
    }
}

[TestFixture]
public class CorrelationIdResolverTests
{
    [Test]
    public void ShouldReturnHeaderValue_WhenValidHeaderProvided()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdConstants.HeaderName] = "abc-123";

        CorrelationIdResolver.Resolve(context).ShouldBe("abc-123");
    }

    [Test]
    public void ShouldGenerateGuid_WhenHeaderMissing()
    {
        var context = new DefaultHttpContext();

        Guid.TryParse(CorrelationIdResolver.Resolve(context), out _).ShouldBeTrue();
    }

    [Test]
    public void ShouldGenerateGuid_WhenHeaderTooLong()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationIdConstants.HeaderName] = new string('x', 129);

        Guid.TryParse(CorrelationIdResolver.Resolve(context), out _).ShouldBeTrue();
    }
}

[TestFixture]
public class SerilogExtensionsAdditionalTests
{
    [Test]
    public void ShouldConfigureSerilog_WhenHostApplicationBuilderUsed()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.AddSerilogJsonConsole();

        using var host = builder.Build();
        host.Services.GetRequiredService<ILoggerFactory>().ShouldNotBeNull();
    }
}

internal sealed class StubConfiguration(string logDirectory) : IConfiguration
{
    public string? this[string key]
    {
        get => key == "LocalTelemetry:LogDirectory" ? logDirectory : null;
        set => throw new NotSupportedException();
    }

    public IEnumerable<IConfigurationSection> GetChildren() => [];

    public IConfigurationSection GetSection(string key) => new StubSection(this, key);

    public IChangeToken GetReloadToken() => NullChangeToken.Singleton;

    private sealed class StubSection(StubConfiguration root, string key) : IConfigurationSection
    {
        public string Key => key.Split(':').Last();
        public string Path => key;
        public string? Value { get => root[key]; set => root[key] = value!; }

        public string? this[string subKey]
        {
            get => root[$"{Path}:{subKey}"];
            set => throw new NotSupportedException();
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];

        public IConfigurationSection GetSection(string subKey) => new StubSection(root, $"{Path}:{subKey}");

        public IChangeToken GetReloadToken() => NullChangeToken.Singleton;
    }

    private sealed class NullChangeToken : IChangeToken
    {
        public static NullChangeToken Singleton { get; } = new();
        public bool HasChanged => false;
        public bool ActiveChangeCallbacks => false;
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) =>
            EmptyDisposable.Instance;
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new();
        public void Dispose()
        {
        }
    }
}
