using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.LlmGateway;
using MediatR;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
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
        var handler = new WorkOrderChatHandler(factory, tool, NullLogger<WorkOrderChatHandler>.Instance);
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

    [Test]
    public async Task Handle_WhenClientThrowsHttpRequestException_ReturnsFriendlyMessage()
    {
        var factory = new ThrowingChatClientFactory(new HttpRequestException("content_filter"));
        var tool = new WorkOrderTool(new StubBus());
        var handler = new WorkOrderChatHandler(factory, tool, NullLogger<WorkOrderChatHandler>.Instance);
        var workOrder = new WorkOrder { Number = "WO-99", Title = "Test" };
        var query = new WorkOrderChatQuery("bad prompt", workOrder);

        var response = await handler.Handle(query, CancellationToken.None);

        response.Text.ShouldBe(WorkOrderChatHandler.FriendlyProviderErrorMessage);
    }

    [Test]
    public async Task Handle_WhenClientThrowsTaskCanceledException_ReturnsFriendlyMessage()
    {
        var factory = new ThrowingChatClientFactory(new TaskCanceledException("timeout"));
        var tool = new WorkOrderTool(new StubBus());
        var handler = new WorkOrderChatHandler(factory, tool, NullLogger<WorkOrderChatHandler>.Instance);
        var workOrder = new WorkOrder { Number = "WO-99", Title = "Test" };
        var query = new WorkOrderChatQuery("slow prompt", workOrder);

        var response = await handler.Handle(query, CancellationToken.None);

        response.Text.ShouldBe(WorkOrderChatHandler.FriendlyProviderErrorMessage);
    }

    private sealed class ThrowingChatClientFactory(Exception exception) : ChatClientFactory(null!)
    {
        public override Task<IChatClient> GetChatClient() =>
            Task.FromResult<IChatClient>(new ThrowingChatClient(exception));
    }

    private sealed class ThrowingChatClient(Exception exception) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw exception;

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
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
