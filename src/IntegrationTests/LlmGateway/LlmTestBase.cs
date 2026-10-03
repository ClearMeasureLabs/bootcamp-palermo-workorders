using System.Net;
using ClearMeasure.Bootcamp.LlmGateway;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

public abstract class LlmTestBase : IntegratedTestBase
{
    private const string ClientResultExceptionFullName = "System.ClientModel.ClientResultException";

    /// <summary>
    /// Ignores a live-LLM test (one marked <see cref="LlmTestAttribute"/>) when no chat client is configured.
    /// Stub-based tests in the same fixture always run.
    /// </summary>
    [SetUp]
    public async Task SkipWhenChatClientUnavailable()
    {
        if (TestContext.CurrentContext.Test.Method?.IsDefined<LlmTestAttribute>(false) != true)
        {
            return;
        }

        var factory = TestHost.GetRequiredService<ChatClientFactory>();
        var availability = await factory.IsChatClientAvailable();

        if (!availability.IsAvailable)
        {
            Assert.Ignore(availability.Message);
        }
    }

    /// <summary>
    /// Azure OpenAI occasionally returns HTTP 429 during CI; skip instead of failing the build.
    /// </summary>
    protected static async Task<T> ExecuteLlmAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            ThrowIfAzureOpenAiRateLimited(ex);
            throw;
        }
    }

    private static void ThrowIfAzureOpenAiRateLimited(Exception ex)
    {
        if (!IsAzureOpenAiRateLimited(ex))
        {
            return;
        }

        Assert.Ignore($"Skipped: Azure OpenAI rate limited (HTTP 429). {ex.Message}");
    }

    private static bool IsAzureOpenAiRateLimited(Exception ex)
    {
        for (var e = ex; e is not null; e = e.InnerException)
        {
            if (e is HttpRequestException { StatusCode: HttpStatusCode.TooManyRequests })
            {
                return true;
            }

            if (string.Equals(e.GetType().FullName, ClientResultExceptionFullName, StringComparison.Ordinal)
                && e.Message.Contains("429", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
