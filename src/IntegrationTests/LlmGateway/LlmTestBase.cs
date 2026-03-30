using ClearMeasure.Bootcamp.LlmGateway;

namespace ClearMeasure.Bootcamp.IntegrationTests.LlmGateway;

/// <summary>
/// Base for tests that require a live LLM. GitHub Actions excludes the <c>LlmGateway</c> test namespace in <c>build.ps1</c> (<c>FullyQualifiedName!~...</c>).
/// </summary>
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
