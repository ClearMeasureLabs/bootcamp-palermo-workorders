namespace ClearMeasure.Bootcamp.UI.Server;

/// <summary>
/// Named rate limiting policies registered for API endpoints.
/// </summary>
public static class ApiRateLimitingPolicyNames
{
    /// <summary>
    /// Sliding-window limiter scoped to <c>/api/*</c> routes (see <c>AddApiRateLimiting</c>).
    /// </summary>
    public const string ApiSlidingWindow = "ApiSlidingWindow";
}
