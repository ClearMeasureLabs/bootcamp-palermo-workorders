using ClearMeasure.Bootcamp.LlmGateway;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

public abstract class LlmTestBase : IntegratedTestBase
{
    [SetUp]
    public void SkipWhenChatClientUnavailable()
    {
        var factory = TestHost.GetRequiredService<ChatClientFactory>();

        if (!factory.IsChatClientAvailable)
        {
            Assert.Ignore("Chat client is not configured. Set AI_OpenAI_ApiKey, AI_OpenAI_Url, and AI_OpenAI_Model to run these tests.");
        }
    }
}
