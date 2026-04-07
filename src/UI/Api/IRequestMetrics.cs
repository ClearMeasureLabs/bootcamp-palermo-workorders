namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Thread-safe counter for HTTP requests observed by the host (one increment per incoming request).
/// </summary>
public interface IRequestMetrics
{
    /// <summary>Increments the count once for an incoming HTTP request.</summary>
    void Increment();

    /// <summary>Total requests counted since process start.</summary>
    long TotalCount { get; }
}
