using System.Net;

namespace ClearMeasure.Bootcamp.IntegrationTests;

/// <summary>
/// Shared handling for Azure OpenAI throttling in tests that call the live API.
/// </summary>
public static class AzureOpenAiRateLimitTestSupport
{
    private const string ClientResultExceptionFullName = "System.ClientModel.ClientResultException";

    /// <summary>
    /// Skips the current test when the exception indicates Azure OpenAI rate limiting (HTTP 429).
    /// </summary>
    public static void ThrowIfRateLimited(Exception ex)
    {
        if (!IsRateLimited(ex))
        {
            return;
        }

        Assert.Ignore($"Skipped: Azure OpenAI rate limited (HTTP 429). {ex.Message}");
    }

    /// <summary>
    /// Returns true when <paramref name="ex"/> or an inner exception indicates HTTP 429 from Azure OpenAI.
    /// </summary>
    public static bool IsRateLimited(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is HttpRequestException http && http.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return true;
            }

            if (string.Equals(e.GetType().FullName, ClientResultExceptionFullName, StringComparison.Ordinal))
            {
                var msg = e.Message;
                if (msg.Contains("429", StringComparison.Ordinal)
                    || msg.Contains("too_many_requests", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
