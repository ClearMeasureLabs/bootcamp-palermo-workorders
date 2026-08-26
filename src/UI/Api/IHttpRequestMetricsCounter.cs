namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Tracks how many HTTP requests this process has served since startup.
/// </summary>
public interface IHttpRequestMetricsCounter
{
    /// <summary>Increments the served-request count by one.</summary>
    void Increment();

    /// <summary>Total requests recorded so far.</summary>
    long Total { get; }
}
