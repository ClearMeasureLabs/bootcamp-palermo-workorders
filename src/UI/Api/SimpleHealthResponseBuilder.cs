using System.Diagnostics;

namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Builds <see cref="SimpleHealthResponse"/> for <c>GET /api/health</c>.
/// </summary>
public static class SimpleHealthResponseBuilder
{
    /// <summary>
    /// Creates a response using the process start time from <see cref="Process.GetCurrentProcess"/>.
    /// </summary>
    public static SimpleHealthResponse Build(TimeProvider timeProvider)
    {
        var processStartUtc = new DateTimeOffset(Process.GetCurrentProcess().StartTime).ToUniversalTime();
        return Build(timeProvider, processStartUtc);
    }

    /// <summary>
    /// Creates a response for a known process start instant (UTC), for tests and deterministic scenarios.
    /// </summary>
    public static SimpleHealthResponse Build(TimeProvider timeProvider, DateTimeOffset processStartUtcUtc)
    {
        var now = timeProvider.GetUtcNow();
        var startUtc = processStartUtcUtc.ToUniversalTime();
        var uptime = now - startUtc;

        return new SimpleHealthResponse
        {
            Status = SimpleHealthStatus.Healthy,
            CurrentTimeUtc = now.UtcDateTime,
            Uptime = uptime
        };
    }
}
