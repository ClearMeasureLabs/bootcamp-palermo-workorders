namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>, reflecting key properties of the incoming HTTP request.
/// </summary>
/// <param name="Method">HTTP method of the incoming request.</param>
/// <param name="Path">Request path value (for example <c>/api/echo</c>).</param>
/// <param name="PathBase">Optional path base when the application is hosted under a virtual directory.</param>
/// <param name="QueryString">Raw query string including the leading <c>?</c> when present.</param>
/// <param name="Query">Parsed query parameters; each key maps to all submitted values.</param>
/// <param name="Scheme">Request scheme (for example <c>https</c>).</param>
/// <param name="Host">Request host value.</param>
/// <param name="RemoteIp">Remote client IP address when available.</param>
/// <param name="Headers">Request header names mapped to value arrays. Sensitive headers such as <c>Authorization</c>, <c>Cookie</c>, and <c>X-Api-Key</c> are redacted to <c>[REDACTED]</c>.</param>
/// <param name="CorrelationId">Correlation identifier from middleware or the incoming <c>X-Correlation-ID</c> header.</param>
public sealed record EchoResponse(
    string Method,
    string Path,
    string? PathBase,
    string? QueryString,
    IReadOnlyDictionary<string, string[]> Query,
    string Scheme,
    string Host,
    string? RemoteIp,
    IReadOnlyDictionary<string, string[]> Headers,
    string? CorrelationId);
