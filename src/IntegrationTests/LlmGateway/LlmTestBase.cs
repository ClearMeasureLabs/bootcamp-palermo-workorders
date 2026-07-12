using System.Net;
using System.Net.Http;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.Configuration;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

public abstract class LlmTestBase : IntegratedTestBase
{
    private const string ClientResultExceptionFullName = "System.ClientModel.ClientResultException";

    /// <summary>
    /// Category for tests that require a real LLM (natural-language reasoning / tool-calling)
    /// and therefore cannot run against the deterministic offline fake. They run only when a
    /// real Azure OpenAI key is configured. Used for CI filtering; the runtime skip is driven
    /// by <see cref="RequiresRealLlm"/> (class-level categories are not visible in [SetUp]).
    /// </summary>
    protected const string LiveLlmCategory = "LiveLlm";

    /// <summary>
    /// Override to true in fixtures whose assertions depend on genuine model reasoning or
    /// tool-calling. Such tests are skipped when the deterministic offline fake is active.
    /// </summary>
    protected virtual bool RequiresRealLlm => false;

    [SetUp]
    public async Task SkipWhenChatClientUnavailable()
    {
        var usingFake = TestHost.GetRequiredService<IConfiguration>().GetValue<bool>("AI_OpenAI_UseFake");

        if (usingFake && RequiresRealLlm)
        {
            Assert.Ignore("Requires a real LLM (LiveLlm); the deterministic offline fake cannot reason or call tools.");
        }

        var factory = TestHost.GetRequiredService<IChatClientFactory>();
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

    /// <summary>
    /// Azure OpenAI occasionally returns HTTP 429 during CI; skip instead of failing the build.
    /// </summary>
    protected static async Task ExecuteLlmAsync(Func<Task> action)
    {
        try
        {
            await action();
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
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is HttpRequestException http && http.StatusCode == HttpStatusCode.TooManyRequests)
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
