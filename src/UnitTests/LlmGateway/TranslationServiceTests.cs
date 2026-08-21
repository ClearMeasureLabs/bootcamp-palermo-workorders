using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;
using Microsoft.Extensions.AI;
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
    public async Task ShouldReturnOriginalTextWhenGetChatClientThrows()
    {
        var service = new TranslationService(new ThrowingChatClientFactory());

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
    public async Task ShouldReturnOriginalTextWhenLanguageCodeIsInvalid()
    {
        var bus = new StubBus(available: true);
        var factory = new ChatClientFactory(bus);
        var service = new TranslationService(factory);

        var result = await service.TranslateAsync("Hello", "'; DROP TABLE Users;--");

        result.ShouldBe("Hello");
        bus.ChatClientConfigQueryCount.ShouldBe(0);
    }

    [Test]
    public async Task ShouldReturnTranslatedTextWhenChatClientSucceeds()
    {
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "  Hola  ")]);
        var service = new TranslationService(new StubChatClientFactory(response));

        var result = await service.TranslateAsync("Hello", "es-ES");

        result.ShouldBe("Hola");
    }

    [Test]
    public async Task ShouldReturnOriginalTextWhenTranslationIsWhitespace()
    {
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, "   ")]);
        var service = new TranslationService(new StubChatClientFactory(response));

        var result = await service.TranslateAsync("Hello", "es-ES");

        result.ShouldBe("Hello");
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

    private sealed class StubChatClientFactory(ChatResponse response) : ChatClientFactory(null!)
    {
        public override Task<IChatClient> GetChatClient() =>
            Task.FromResult<IChatClient>(new StubTranslationChatClient(response));
    }

    private sealed class ThrowingChatClientFactory() : ChatClientFactory(null!)
    {
        public override Task<IChatClient> GetChatClient() =>
            throw new InvalidOperationException("chat-client-unavailable");
    }

    private sealed class StubTranslationChatClient(ChatResponse response) : IChatClient
    {
        public void Dispose()
        {
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(response);

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
