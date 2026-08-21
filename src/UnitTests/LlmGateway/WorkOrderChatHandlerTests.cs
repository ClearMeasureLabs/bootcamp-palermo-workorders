using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.LlmGateway;
using MediatR;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class WorkOrderChatHandlerTests
{
    [Test]
    public async Task Handle_ShouldReturnChatResponseFromStubClient()
    {
        var bus = new StubBus();
        var factory = new StubChatClientFactory(bus, "stub-reply");
        var tool = new WorkOrderTool(bus);
        var handler = new WorkOrderChatHandler(factory, tool);
        var workOrder = new WorkOrder { Number = "WO-42", Title = "Paint" };
        var query = new WorkOrderChatQuery("What is status?", workOrder);

        var response = await handler.Handle(query, CancellationToken.None);

        response.Text.ShouldBe("stub-reply");
        factory.GetChatClientCallCount.ShouldBe(1);
        factory.LastMessages.ShouldNotBeNull();
        factory.LastMessages!.Any(m => m.Role == ChatRole.System && m.Text.Contains("WO-42")).ShouldBeTrue();
        factory.LastMessages!.Any(m => m.Role == ChatRole.User && m.Text == "What is status?").ShouldBeTrue();
        factory.LastOptions.ShouldNotBeNull();
        factory.LastOptions!.Tools.ShouldNotBeNull();
        factory.LastOptions.Tools!.Count.ShouldBe(2);
    }

    private sealed class StubChatClientFactory(IBus bus, string reply) : ChatClientFactory(bus)
    {
        public int GetChatClientCallCount { get; private set; }
        public IList<ChatMessage>? LastMessages { get; private set; }
        public ChatOptions? LastOptions { get; private set; }

        public override Task<IChatClient> GetChatClient()
        {
            GetChatClientCallCount++;
            return Task.FromResult<IChatClient>(new StubChatClient(reply, this));
        }

        public void Capture(IEnumerable<ChatMessage> messages, ChatOptions? options)
        {
            LastMessages = messages.ToList();
            LastOptions = options;
        }
    }

    private sealed class StubChatClient(string reply, StubChatClientFactory factory) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            factory.Capture(messages, options);
            return Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, reply)]));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class StubBus : IBus
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request) =>
            throw new NotImplementedException();

        public Task<object?> Send(object request) =>
            throw new NotImplementedException();

        public Task Publish(INotification notification) => Task.CompletedTask;
    }
}
