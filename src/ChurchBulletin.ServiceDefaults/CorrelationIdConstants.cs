namespace ClearMeasure.Bootcamp.ServiceDefaults;

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
    /// Key used in <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> for the current correlation identifier.
    /// </summary>
    public const string HttpContextItemKey = "CorrelationId";
}
