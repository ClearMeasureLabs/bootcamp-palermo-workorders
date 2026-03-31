namespace ClearMeasure.Bootcamp.UI.Shared;

/// <summary>
/// Shared names for optional API key authentication between server middleware and HTTP clients.
/// </summary>
public static class ApiKeyConstants
{
    /// <summary>
    /// Request header carrying the shared secret for machine clients.
    /// </summary>
    public const string HeaderName = "X-Api-Key";
}

/// <summary>
/// Routes for the Blazor WASM abstract web-service endpoint (MediatR-over-HTTP).
/// </summary>
public static class WebServiceApiRoutes
{
    /// <summary>
    /// Path segment after <c>api/</c> (no leading slash).
    /// </summary>
    public const string AbstractPathSegment = "blazor-wasm-single-api";

    /// <summary>
    /// Legacy unversioned URL path (no leading slash).
    /// </summary>
    public const string LegacyRelativeUrl = "api/" + AbstractPathSegment;
}
