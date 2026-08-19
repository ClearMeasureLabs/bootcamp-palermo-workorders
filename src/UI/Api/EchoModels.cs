namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>.
/// </summary>
/// <param name="Method">HTTP method of the incoming request.</param>
/// <param name="Path">Request path (excluding query string).</param>
/// <param name="QueryString">Raw query string including leading <c>?</c>, or empty when absent.</param>
/// <param name="Query">Parsed query parameters (multiple values per key preserved).</param>
/// <param name="Headers">Selected request headers; sensitive values are redacted.</param>
/// <param name="ClientIp">Client IP from the connection or first <c>X-Forwarded-For</c> hop.</param>
public record EchoResponse(
    string Method,
    string Path,
    string QueryString,
    IReadOnlyDictionary<string, string[]> Query,
    IReadOnlyDictionary<string, string> Headers,
    string? ClientIp);
