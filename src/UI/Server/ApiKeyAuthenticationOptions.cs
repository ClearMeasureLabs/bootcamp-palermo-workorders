namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Configuration for optional <c>X-Api-Key</c> validation on HTTP API routes under <c>/api/</c>.
/// </summary>
public sealed class ApiKeyAuthenticationOptions
{
    public const string SectionName = "ApiKeyAuthentication";

    /// <summary>
    /// When <c>true</c> and <see cref="ValidationKey"/> is non-empty, requests to protected API paths must send a matching <c>X-Api-Key</c> header.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Secret value callers must send in the <c>X-Api-Key</c> header. Whitespace is trimmed when reading configuration.
    /// </summary>
    public string? ValidationKey { get; set; }
}
