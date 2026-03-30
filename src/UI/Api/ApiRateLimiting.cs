namespace ClearMeasure.Bootcamp.UI.Api;

/// <summary>
/// Policy name for sliding-window API rate limiting; must match the limiter registered in UI.Server.
/// </summary>
public static class ApiRateLimiting
{
    public const string PolicyName = "ApiSlidingWindow";
}
