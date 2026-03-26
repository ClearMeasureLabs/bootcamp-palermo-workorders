namespace ClearMeasure.Bootcamp.UI.Api.Controllers;

/// <summary>
/// JSON payload returned by <c>GET /api/echo</c> with safe reflections of the incoming HTTP request.
/// </summary>
public sealed class EchoResponse
{
    /// <summary>HTTP method (e.g. GET).</summary>
    public required string Method { get; init; }

    /// <summary>Request path from <see cref="Microsoft.AspNetCore.Http.HttpRequest.Path"/>.</summary>
    public required string Path { get; init; }

    /// <summary>Query string parameters; duplicate keys keep the last value.</summary>
    public required IReadOnlyDictionary<string, string> QueryParameters { get; init; }

    /// <summary>Allowlisted request headers only; never includes Authorization, Cookie, or other sensitive names.</summary>
    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    /// <summary>Client IP from <see cref="Microsoft.AspNetCore.Http.ConnectionInfo.RemoteIpAddress"/>, if present.</summary>
    public string? RemoteIp { get; init; }

    /// <summary>UTC instant when the response was built.</summary>
    public required DateTime Timestamp { get; init; }
}
