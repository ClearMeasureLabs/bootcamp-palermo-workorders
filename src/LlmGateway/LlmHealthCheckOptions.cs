namespace ClearMeasure.Bootcamp.LlmGateway;

public class LlmHealthCheckOptions
{
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);
}
