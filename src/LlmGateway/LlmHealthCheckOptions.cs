namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// Configuration options for the <see cref="CanConnectToLlmServerHealthCheck"/> probe cache.
/// </summary>
public class LlmHealthCheckOptions
{
    /// <summary>
    /// How long a successful probe result is cached before the health check re-queries the LLM.
    /// Defaults to 5 minutes. Bind from <c>LlmHealthCheck:CacheDuration</c> in appsettings.
    /// </summary>
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromMinutes(5);
}
