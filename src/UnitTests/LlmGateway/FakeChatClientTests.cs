using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.LlmGateway;
using ClearMeasure.Bootcamp.UI.Shared;
using MediatR;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class FakeChatClientTests
{
    [Test]
    public async Task FactoryInFakeMode_ReportsAvailable_AndReturnsFakeClient()
    {
        var factory = new ChatClientFactory(new FakeModeBus());

        var availability = await factory.IsChatClientAvailable();
        var client = await factory.GetChatClient();

        availability.IsAvailable.ShouldBeTrue();
        client.ShouldBeOfType<FakeChatClient>();
    }

    [Test]
    public async Task FakeClient_AnswersHealthProbe_WithOk()
    {
        var client = new FakeChatClient();

        var response = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "Reply with OK")]);

        response.Messages.ShouldNotBeEmpty();
        response.Text.Trim().ShouldBe("OK");
    }

    [Test]
    public async Task FakeClient_ReturnsNonEmptyResult_ThatDiffersFromInput()
    {
        var client = new FakeChatClient();
        const string input = "Fix the broken pipe";

        var response = await client.GetResponseAsync(
        [
            new ChatMessage(ChatRole.System, "Translate the following text into Spanish."),
            new ChatMessage(ChatRole.User, input)
        ]);

        response.Text.ShouldNotBeNullOrWhiteSpace();
        response.Text.ShouldNotBe(input);
    }

    [Test]
    public async Task TranslationService_WithFakeFactory_ReturnsTranslatedNonOriginalText()
    {
        var service = new TranslationService(new ChatClientFactory(new FakeModeBus()));

        var result = await service.TranslateAsync("Fix the broken pipe", "es-ES");

        result.ShouldNotBeNullOrWhiteSpace();
        result.ShouldNotBe("Fix the broken pipe");
    }

    private class FakeModeBus() : Bus(null!)
    {
        public override Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            if (request is ChatClientConfigQuery)
            {
                var config = new ChatClientConfig
                {
                    AiOpenAiApiKey = null,
                    AiOpenAiUrl = null,
                    AiOpenAiModel = null,
                    UseFake = true
                };
                return Task.FromResult((TResponse)(object)config);
            }

            throw new NotImplementedException($"Unhandled request type: {request.GetType().Name}");
        }

        public override Task Publish(INotification notification) => Task.CompletedTask;
    }
}
