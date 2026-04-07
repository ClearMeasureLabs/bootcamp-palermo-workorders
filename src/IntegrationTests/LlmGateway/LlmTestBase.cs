using ClearMeasure.Bootcamp.LlmGateway;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

public abstract class LlmTestBase : IntegratedTestBase
{
    [SetUp]
    public async Task SkipWhenChatClientUnavailable()
    {
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
            AzureOpenAiRateLimitTestSupport.ThrowIfRateLimited(ex);
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
            AzureOpenAiRateLimitTestSupport.ThrowIfRateLimited(ex);
            throw;
        }
    }
}
