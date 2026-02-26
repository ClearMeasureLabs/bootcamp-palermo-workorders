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
}
