using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class TranslationServiceTests
{
    [Test]
    public async Task ShouldReturnOriginalTextWhenTargetLanguageIsEnUS()
    {
        var bus = new StubBus(available: true);
        var factory = new ChatClientFactory(bus);
        var service = new TranslationService(factory);

        var result = await service.TranslateAsync("Fix the pipe", "en-US");

        result.ShouldBe("Fix the pipe");
        bus.ChatClientConfigQueryCount.ShouldBe(0);
    }

    [Test]
    public async Task ShouldReturnOriginalTextWhenChatClientUnavailable()
    {
        var bus = new StubBus(available: false);
        var factory = new ChatClientFactory(bus);
        var service = new TranslationService(factory);

        var result = await service.TranslateAsync("Hello", "es-ES");

        result.ShouldBe("Hello");
    }

    [Test]
    public async Task ShouldReturnOriginalTextWhenInputIsEmpty()
    {
        var bus = new StubBus(available: true);
        var factory = new ChatClientFactory(bus);
        var service = new TranslationService(factory);

        var result = await service.TranslateAsync("", "es-ES");

        result.ShouldBe("");
    }

    [Test]
    public async Task ShouldReturnOriginalTextWhenInputIsNull()
    {
        var bus = new StubBus(available: true);
        var factory = new ChatClientFactory(bus);
        var service = new TranslationService(factory);

        var result = await service.TranslateAsync(null!, "es-ES");

        result.ShouldBe(string.Empty);
    }

    [Test]
    public async Task ShouldReturnOriginalTextWhenLanguageCodeIsInvalid()
    {
        var bus = new StubBus(available: true);
        var factory = new ChatClientFactory(bus);
        var service = new TranslationService(factory);

        var result = await service.TranslateAsync("Hello", "'; DROP TABLE Users;--");

        result.ShouldBe("Hello");
        bus.ChatClientConfigQueryCount.ShouldBe(0);
    }

    private class StubBus(bool available) : Bus(null!)
    {
        public int ChatClientConfigQueryCount { get; private set; }

        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is ChatClientConfigQuery)
            {
                ChatClientConfigQueryCount++;
                var config = new ChatClientConfig
                {
                    AiOpenAiApiKey = available ? "test-key" : "",
                    AiOpenAiUrl = available ? "https://test.openai.azure.com" : "",
                    AiOpenAiModel = available ? "gpt-4" : ""
                };
                return Task.FromResult((TResponse)(object)config);
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }

        public override Task Publish(INotification notification) => Task.CompletedTask;
    }
}
