namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// JSON payload for <c>GET /api/echo</c> and <c>GET /api/v1.0/echo</c>.
/// </summary>
public sealed record EchoResponse(
    string Method,
    string Path,
    string QueryString,
    string Scheme,
    string Host,
    string Protocol,
    string? RemoteIp,
    IReadOnlyDictionary<string, string> Headers,
    IReadOnlyDictionary<string, string[]> Query);
