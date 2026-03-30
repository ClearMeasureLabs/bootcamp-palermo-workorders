namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Route templates for HTTP APIs. Contract versions use a path segment after <c>api/</c>
/// (for example <c>/api/v1.0/health</c>). Build metadata remains at <c>/api/version</c> as documented on the controller.
/// </summary>
public static class ApiRoutes
{
    /// <summary>
    /// Prefix for versioned API routes: <c>api/v{version:apiVersion}</c>.
    /// </summary>
    public const string VersionedApiPrefix = "api/v{version:apiVersion}";
}
