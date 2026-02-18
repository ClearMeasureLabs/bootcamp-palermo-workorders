using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Background service that writes telemetry data to local text files.
/// </summary>
public class LocalTelemetryFileWriter : BackgroundService, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private bool _disposed;

    public string TelemetryLogDirectory { get; }

    private readonly ActivityListener _activityListener;
    private readonly MeterListener _meterListener;
    private readonly object _tracesLock = new();
    private readonly object _eventsLock = new();
    private readonly object _logsLock = new();
    private readonly object _metricsLock = new();

    private StreamWriter? _tracesWriter;
    private StreamWriter? _eventsWriter;
    private StreamWriter? _logsWriter;
    private StreamWriter? _metricsWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalTelemetryFileWriter"/> class.
    /// </summary>
    public LocalTelemetryFileWriter(IConfiguration? configuration = null)
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = OnActivityStarted,
            ActivityStopped = OnActivityStopped
        };

        _meterListener = new MeterListener
        {
            InstrumentPublished = OnInstrumentPublished
        };
        _meterListener.SetMeasurementEventCallback<int>(OnMeasurementRecorded);
        _meterListener.SetMeasurementEventCallback<long>(OnMeasurementRecorded);
        _meterListener.SetMeasurementEventCallback<float>(OnMeasurementRecorded);
        _meterListener.SetMeasurementEventCallback<double>(OnMeasurementRecorded);
        _meterListener.SetMeasurementEventCallback<decimal>(OnMeasurementRecorded);

        var configuredPath = configuration?["LocalTelemetry:LogDirectory"];

        if (!string.IsNullOrEmpty(configuredPath))
        {
            TelemetryLogDirectory = configuredPath;
        }
        else
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Directory.GetCurrentDirectory();
            TelemetryLogDirectory = Path.Combine(basePath, "logs");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Directory.Exists(TelemetryLogDirectory))
        {
            Directory.CreateDirectory(TelemetryLogDirectory);
        }

        CleanupOldFiles();

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
        _tracesWriter = new StreamWriter(Path.Combine(TelemetryLogDirectory, $"traces_{timestamp}.jsonl"), append: true) { AutoFlush = true };
        _eventsWriter = new StreamWriter(Path.Combine(TelemetryLogDirectory, $"events_{timestamp}.jsonl"), append: true) { AutoFlush = true };
        _logsWriter = new StreamWriter(Path.Combine(TelemetryLogDirectory, $"logs_{timestamp}.jsonl"), append: true) { AutoFlush = true };
        _metricsWriter = new StreamWriter(Path.Combine(TelemetryLogDirectory, $"metrics_{timestamp}.jsonl"), append: true) { AutoFlush = true };

        ActivitySource.AddActivityListener(_activityListener);
        _meterListener.Start();

        Trace.WriteLine($"Local telemetry file writer started. Writing to {Path.GetFullPath(TelemetryLogDirectory)}");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Expected when stopping
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await DisposeAsync();
        await base.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _activityListener.Dispose();
        _meterListener.Dispose();

        if (_tracesWriter != null) await _tracesWriter.DisposeAsync();
        if (_eventsWriter != null) await _eventsWriter.DisposeAsync();
        if (_logsWriter != null) await _logsWriter.DisposeAsync();
        if (_metricsWriter != null) await _metricsWriter.DisposeAsync();

        GC.SuppressFinalize(this);
    }

    public void WriteTraceEntry(Activity activity, string status)
    {
        if (_tracesWriter == null) return;

        var entry = new TraceEntry(activity, status);

        lock (_tracesLock)
        {
            try
            {
                _tracesWriter.WriteLine(JsonSerializer.Serialize(entry, JsonOptions));
            }
            catch
            {
                // Ignore write errors to prevent affecting application
            }
        }
    }

    public void WriteEventEntry(Activity activity, ActivityEvent evt)
    {
        if (_eventsWriter == null) return;

        var entry = new EventEntry(activity, evt);

        lock (_eventsLock)
        {
            try
            {
                _eventsWriter.WriteLine(JsonSerializer.Serialize(entry, JsonOptions));
            }
            catch
            {
                // Ignore write errors to prevent affecting application
            }
        }
    }

    public void WriteLogEntry(LogLevel level, string category, string message, Exception? exception = null)
    {
        if (_logsWriter == null) return;

        var entry = new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level.ToString(),
            Category = category,
            Message = message,
            Exception = exception != null ? new LogEntryError(exception) : null
        };

        lock (_logsLock)
        {
            try
            {
                _logsWriter.WriteLine(JsonSerializer.Serialize(entry, JsonOptions));
            }
            catch
            {
                // Ignore write errors to prevent affecting application
            }
        }
    }

    public void WriteMetricEntry(string name, double value, string unit = "", IDictionary<string, object?>? tags = null)
    {
        if (_metricsWriter == null) return;

        var entry = new MetricEntry(name, value, unit, tags);

        lock (_metricsLock)
        {
            try
            {
                _metricsWriter.WriteLine(JsonSerializer.Serialize(entry, JsonOptions));
            }
            catch
            {
                // Ignore write errors to prevent affecting application
            }
        }
    }

    private void CleanupOldFiles(int retentionDays = 7)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            foreach (var file in Directory.GetFiles(TelemetryLogDirectory, "*.jsonl"))
            {
                if (File.GetCreationTimeUtc(file) < cutoffDate)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private void OnActivityStarted(Activity activity)
    {
        WriteTraceEntry(activity, "STARTED");
    }

    private void OnActivityStopped(Activity activity)
    {
        WriteTraceEntry(activity, "STOPPED");

        foreach (var evt in activity.Events)
        {
            WriteEventEntry(activity, evt);
        }
    }

    private void OnInstrumentPublished(Instrument instrument, MeterListener listener)
    {
        // Listen to all instruments
        listener.EnableMeasurementEvents(instrument);
    }

    private void OnMeasurementRecorded<T>(
        Instrument instrument,
        T measurement,
        ReadOnlySpan<KeyValuePair<string, object?>> tags,
        object? state) where T : struct
    {
        var tagsDictionary = new Dictionary<string, object?>();

        foreach (var tag in tags)
        {
            tagsDictionary[tag.Key] = tag.Value;
        }

        tagsDictionary["meter.name"] = instrument.Meter.Name;
        tagsDictionary["meter.version"] = instrument.Meter.Version;
        tagsDictionary["instrument.type"] = instrument.GetType().Name;

        var value = Convert.ToDouble(measurement);
        WriteMetricEntry(instrument.Name, value, instrument.Unit ?? "", tagsDictionary);
    }
}
