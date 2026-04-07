using System.Net;
using System.Net.Http;
using NUnit.Framework;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

/// <summary>
/// Azure OpenAI occasionally returns HTTP 429 during CI; NUnit tests can skip instead of failing the build.
/// </summary>
public static class AzureOpenAiRateLimitGuard
{
    private const string ClientResultExceptionFullName = "System.ClientModel.ClientResultException";

    /// <summary>
    /// If <paramref name="ex"/> indicates Azure OpenAI rate limiting, marks the test inconclusive via <see cref="Assert.Ignore(string)"/>.
    /// Otherwise returns normally so the caller can rethrow.
    /// </summary>
    public static void ThrowIfRateLimited(Exception ex)
    {
        if (IsAzureOpenAiRateLimited(ex))
            Assert.Ignore($"Skipped: Azure OpenAI rate limited (HTTP 429). {ex.Message}");
    }

    internal static bool IsAzureOpenAiRateLimited(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is HttpRequestException http && http.StatusCode == HttpStatusCode.TooManyRequests)
                return true;

            if (string.Equals(e.GetType().FullName, ClientResultExceptionFullName, StringComparison.Ordinal)
                && e.Message.Contains("429", StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
