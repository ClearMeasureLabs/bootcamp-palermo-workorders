namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Identifies request headers whose values must not be echoed verbatim.
/// </summary>
internal static class EchoHeaderRedaction
{
    /// <summary>
    /// Placeholder substituted for redacted header values in echo responses.
    /// </summary>
    internal const string RedactedValue = "***REDACTED***";

    /// <summary>
    /// Matches <see cref="ClearMeasure.Bootcamp.UI.Shared.ApiKeyConstants.HeaderName"/> without a UI.Shared reference.
    /// </summary>
    private const string ApiKeyHeaderName = "X-Api-Key";

    private static readonly HashSet<string> SensitiveHeaderNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        ApiKeyHeaderName
    };

    /// <summary>
    /// Returns <see langword="true"/> when the header name must be redacted in echo output.
    /// </summary>
    internal static bool IsSensitive(string headerName) =>
        SensitiveHeaderNames.Contains(headerName);
}
