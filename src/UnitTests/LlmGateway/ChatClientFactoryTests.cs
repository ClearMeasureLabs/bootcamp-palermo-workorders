using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.LlmGateway;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class ChatClientFactoryTests
{
    [Test]
    public async Task IsChatClientAvailable_WhenApiKeyMissing_ReturnsUnavailable()
    {
        var factory = new ChatClientFactory(new StubConfigBus(Config(apiKey: null)));
        var result = await factory.IsChatClientAvailable();

        result.IsAvailable.ShouldBeFalse();
        result.Message.ShouldContain("AI_OpenAI_ApiKey");
    }

    [Test]
    public async Task IsChatClientAvailable_WhenAllAzureSettingsPresent_ReturnsAvailable()
    {
        var factory = new ChatClientFactory(new StubConfigBus(Config()));
        var result = await factory.IsChatClientAvailable();

        result.IsAvailable.ShouldBeTrue();
    }

    [Test]
    public async Task IsChatClientAvailable_WhenOpenAiCompatibleAndComplete_ReturnsAvailable()
    {
        var factory = new ChatClientFactory(new StubConfigBus(Config(provider: "OpenAICompatible")));
        var result = await factory.IsChatClientAvailable();

        result.IsAvailable.ShouldBeTrue();
    }

    [Test]
    public async Task IsChatClientAvailable_WhenIncomplete_MentionsAzureAndCompatibleHints()
    {
        var factory = new ChatClientFactory(new StubConfigBus(Config(url: null)));
        var azure = await factory.IsChatClientAvailable();
        azure.IsAvailable.ShouldBeFalse();
        azure.Message.ShouldContain("Azure OpenAI");

        var factoryCompatible = new ChatClientFactory(new StubConfigBus(Config(provider: "OpenAICompatible", url: null)));
        var compatible = await factoryCompatible.IsChatClientAvailable();
        compatible.IsAvailable.ShouldBeFalse();
        compatible.Message.ShouldContain("OpenAI-compatible");
    }

    private static ChatClientConfig Config(
        string? apiKey = "key",
        string? url = "https://example.openai.azure.com",
        string? model = "gpt-4o",
        string? provider = null) =>
        new()
        {
            AiOpenAiProvider = provider,
            AiOpenAiApiKey = apiKey,
            AiOpenAiUrl = url,
            AiOpenAiModel = model
        };

    private sealed class StubConfigBus(ChatClientConfig config) : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is ChatClientConfigQuery)
            {
                return Task.FromResult((TResponse)(object)config);
            }

            throw new NotSupportedException();
        }

        public Task<object?> Send(object request) => throw new NotSupportedException();
        public Task Publish(INotification notification) => throw new NotSupportedException();
    }
}
