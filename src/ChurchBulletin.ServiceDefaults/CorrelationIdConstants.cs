namespace ChurchBulletin.ServiceDefaults;

/// <summary>
/// Names for correlation identifier propagation over HTTP.
/// </summary>
public static class CorrelationIdConstants
{
    /// <summary>
    /// Request and response header carrying the correlation identifier.
    /// </summary>
    public const string HeaderName = "X-Correlation-ID";

    /// <summary>
    /// Maximum accepted length for an incoming correlation identifier header value.
    /// </summary>
    public const int MaxLength = 128;

    /// <summary>
    /// Key used in <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> for the current correlation identifier.
    /// </summary>
    public const string HttpContextItemKey = "CorrelationId";
}
