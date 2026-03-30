namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Named ASP.NET Core output-cache policies registered in the host application.
/// </summary>
public static class OutputCachePolicyNames
{
    /// <summary>
    /// Build and host metadata; long TTL is acceptable because values change only on redeploy.
    /// </summary>
    public const string VersionMetadata = nameof(VersionMetadata);

    /// <summary>
    /// Sample weather data; short TTL limits how long random demo rows are reused.
    /// </summary>
    public const string WeatherSample = nameof(WeatherSample);
}
