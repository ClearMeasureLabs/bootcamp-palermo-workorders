using ClearMeasure.Bootcamp.LlmGateway;
using MediatR;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class AgentChatHandlerTests
{
    [Test]
    public async Task Should_return_ChatResponse_when_sending_AgentChatQuery()
    {
        var expectedText = "Stub response";
        var stubClient = new StubChatClient(expectedText);
        var stubFactory = new StubChatClientFactory(stubClient);
        var stubTool = new StubWorkOrderTool();

        var handler = new AgentChatHandler(stubFactory, stubTool);
        var query = new AgentChatQuery("Hello", "testuser");

        var result = await handler.Handle(query, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Text.ShouldBe(expectedText);
        stubFactory.GetChatClientCalled.ShouldBeTrue();
        stubClient.MessagesReceived.ShouldNotBeEmpty();
    }

    [Test]
    public async Task Should_include_user_identity_in_system_messages()
    {
        var stubClient = new StubChatClient("OK");
        var stubFactory = new StubChatClientFactory(stubClient);
        var stubTool = new StubWorkOrderTool();

        var handler = new AgentChatHandler(stubFactory, stubTool);
        var query = new AgentChatQuery("Hi", "jdoe");

        await handler.Handle(query, CancellationToken.None);

        var systemMessages = stubClient.MessagesReceived
            .Where(m => m.Role == ChatRole.System)
            .Select(m => m.Text)
            .ToList();
        var hasUserIdentity = systemMessages.Any(c => c.Contains("jdoe", StringComparison.OrdinalIgnoreCase));
        hasUserIdentity.ShouldBeTrue();
    }

    private class StubChatClient(string responseText) : IChatClient, IDisposable
    {
        public List<ChatMessage> MessagesReceived { get; } = new();

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            MessagesReceived.AddRange(messages);
            var response = new ChatResponse
            {
                Messages = [new ChatMessage(ChatRole.Assistant, responseText)]
            };
            return Task.FromResult(response);
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public object? GetService(Type serviceType, object? input = null) => null;

        public void Dispose() { }
    }

    private class StubChatClientFactory(IChatClient client) : ChatClientFactory(null!)
    {
        public bool GetChatClientCalled { get; private set; }

        public override Task<IChatClient> GetChatClient()
        {
            GetChatClientCalled = true;
            return Task.FromResult(client);
        }
    }

    private class StubWorkOrderTool : WorkOrderTool
    {
        public StubWorkOrderTool() : base(new StubBus())
        {
        }
    }

    private class StubBus : ClearMeasure.Bootcamp.Core.IBus
    {
        public Task<TResponse> Send<TResponse>(MediatR.IRequest<TResponse> request) =>
            Task.FromResult<TResponse>(default!);

        public Task<object?> Send(object request) =>
            Task.FromResult<object?>(null);

        public Task Publish(MediatR.INotification notification) =>
            Task.CompletedTask;
    }
}
