namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Cross-origin resource sharing settings loaded from configuration (section <c>Cors</c>).
/// </summary>
public sealed class ServerCorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Policy name registered with <see cref="Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions"/>.
    /// </summary>
    public const string PolicyName = "ServerCors";

    /// <summary>
    /// When false, CORS middleware and endpoint metadata are not applied.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Allowed values for the <c>Origin</c> request header. Ignored when <see cref="Enabled"/> is false.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = [];
}
