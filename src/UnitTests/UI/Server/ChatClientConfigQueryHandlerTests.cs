using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Server;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server;

[TestFixture]
public class ChatClientConfigQueryHandlerTests
{
    [Test]
    public async Task Handle_ShouldReturnConfigurationValues_WithoutConditionalLoggerAccess()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI_OpenAI_ApiKey"] = "test-key",
                ["AI_OpenAI_Url"] = "https://example.test",
                ["AI_OpenAI_Model"] = "test-model"
            })
            .Build();
        var handler = new ChatClientConfigQueryHandler(
            configuration,
            NullLogger<ChatClientConfigQueryHandler>.Instance);

        var result = await handler.Handle(new ChatClientConfigQuery(), CancellationToken.None);

        result.AiOpenAiApiKey.ShouldBe("test-key");
        result.AiOpenAiUrl.ShouldBe("https://example.test");
        result.AiOpenAiModel.ShouldBe("test-model");
    }
}
