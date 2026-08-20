namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c> reflecting inbound HTTP request metadata.
/// </summary>
public sealed record EchoResponse(
    string Method,
    string Path,
    string QueryString,
    IReadOnlyDictionary<string, string?> Query,
    string Scheme,
    string Host,
    string Protocol,
    IReadOnlyDictionary<string, string> Headers);
