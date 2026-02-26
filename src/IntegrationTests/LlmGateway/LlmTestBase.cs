using ClearMeasure.Bootcamp.LlmGateway;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

public abstract class LlmTestBase : IntegratedTestBase
{
    [SetUp]
    public async Task SkipWhenChatClientUnavailable()
    {
        var factory = TestHost.GetRequiredService<ChatClientFactory>();

        if (!await factory.IsChatClientAvailable())
        {
            Assert.Ignore("Chat client is not configured. Set AI_OpenAI_ApiKey, AI_OpenAI_Url, and AI_OpenAI_Model to run these tests.");
        }
    }
}
