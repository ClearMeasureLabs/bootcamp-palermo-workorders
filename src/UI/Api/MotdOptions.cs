namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Configuration-bound message-of-the-day text exposed on <c>GET /api/motd</c>.
/// </summary>
public sealed class MotdOptions
{
    /// <summary>Configuration section name (root <c>Motd</c> in appsettings).</summary>
    public const string SectionName = "Motd";

    /// <summary>Configured message returned in the JSON payload; empty or whitespace yields an empty string.</summary>
    public string Message { get; set; } = string.Empty;
}
