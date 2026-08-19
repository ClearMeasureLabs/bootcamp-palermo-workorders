namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>.
/// Debug-only: reflects non-sensitive request metadata. Headers use an allowlist only;
/// credential-bearing headers (<c>Authorization</c>, <c>Cookie</c>, <c>X-Api-Key</c>, etc.) are never included.
/// Query strings may contain secrets—operators must avoid putting sensitive tokens in query parameters.
/// </summary>
/// <param name="Method">HTTP method (for GET echo, typically GET).</param>
/// <param name="Scheme">Request scheme (http or https).</param>
/// <param name="Host"><see cref="Microsoft.AspNetCore.Http.HostString"/> value (<c>Host</c> header / connection).</param>
/// <param name="Path">Path portion from <see cref="Microsoft.AspNetCore.Http.HttpRequest.Path"/>.</param>
/// <param name="PathBase">Application path base from <see cref="Microsoft.AspNetCore.Http.HttpRequest.PathBase"/>.</param>
/// <param name="QueryString">Raw query string including leading <c>?</c> when present.</param>
/// <param name="Query">Parsed query keys; duplicate keys keep the last value.</param>
/// <param name="RemoteIpAddress">Client remote IP from <see cref="Microsoft.AspNetCore.Http.ConnectionInfo.RemoteIpAddress"/>, if present.</param>
/// <param name="Protocol">HTTP protocol (for example HTTP/1.1).</param>
/// <param name="Headers">
/// Allowlisted header names only:
/// <c>Accept</c>, <c>Accept-Encoding</c>, <c>Accept-Language</c>, <c>User-Agent</c>, <c>Host</c>, <c>Referer</c>,
/// <c>X-Correlation-ID</c>, <c>X-Request-ID</c>, <c>traceparent</c>.
/// </param>
public sealed record EchoResponse(
    string Method,
    string Scheme,
    string Host,
    string Path,
    string PathBase,
    string QueryString,
    IReadOnlyDictionary<string, string> Query,
    string? RemoteIpAddress,
    string Protocol,
    IReadOnlyDictionary<string, string> Headers);
