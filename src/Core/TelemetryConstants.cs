namespace ClearMeasure.Bootcamp.Core;

/// <summary>
/// Constants for OpenTelemetry source names, meter names, and versions used across the application.
/// </summary>
public static class TelemetryConstants
{
    /// <summary>
    /// The application-level activity source and meter name.
    /// </summary>
    public const string ApplicationSourceName = "ChurchBulletin.Application";

    /// <summary>
    /// The LLM gateway activity source name.
    /// </summary>
    public const string LlmGatewaySourceName = "ChurchBulletin.LlmGateway";

    /// <summary>
    /// The application telemetry version.
    /// </summary>
    public const string ApplicationVersion = "1.0.0";
}
