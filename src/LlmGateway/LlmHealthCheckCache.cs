using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ClearMeasure.Bootcamp.LlmGateway;

public interface ILlmHealthCheckCache
{
    bool TryGet(TimeSpan window, out HealthCheckResult cached, out double ageSeconds);
    void Set(HealthCheckResult result, DateTimeOffset now);
}

public sealed class LlmHealthCheckCache(Func<DateTimeOffset>? clock = null) : ILlmHealthCheckCache
{
    private readonly Func<DateTimeOffset> _clock = clock ?? (() => DateTimeOffset.UtcNow);
    private volatile CacheEntry? _entry;

    public bool TryGet(TimeSpan window, out HealthCheckResult cached, out double ageSeconds)
    {
        var entry = _entry;
        if (entry is null)
        {
            cached = default;
            ageSeconds = 0;
            return false;
        }

        var age = _clock() - entry.CapturedAt;
        if (age > window)
        {
            cached = default;
            ageSeconds = 0;
            return false;
        }

        cached = entry.Result;
        ageSeconds = age.TotalSeconds;
        return true;
    }

    public void Set(HealthCheckResult result, DateTimeOffset now)
    {
        _entry = new CacheEntry(result, now);
    }

    private sealed record CacheEntry(HealthCheckResult Result, DateTimeOffset CapturedAt);
}
