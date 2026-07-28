namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c> reflecting key properties of the incoming HTTP request.
/// </summary>
/// <param name="Method">HTTP method of the incoming request.</param>
/// <param name="Scheme">Request scheme (for example <c>https</c>).</param>
/// <param name="Host">Request host value.</param>
/// <param name="Path">Request path.</param>
/// <param name="PathBase">Application path base.</param>
/// <param name="QueryString">Raw query string including leading <c>?</c>, or empty when absent.</param>
/// <param name="Query">First value per query parameter key.</param>
/// <param name="Headers">Selected, non-sensitive request headers for diagnostics.</param>
/// <param name="CorrelationId">Correlation identifier from middleware when present.</param>
/// <param name="TimestampUtc">UTC timestamp when the response was built.</param>
public sealed record EchoResponse(
    string Method,
    string Scheme,
    string Host,
    string Path,
    string PathBase,
    string QueryString,
    IReadOnlyDictionary<string, string> Query,
    IReadOnlyDictionary<string, string> Headers,
    string? CorrelationId,
    DateTimeOffset TimestampUtc);
