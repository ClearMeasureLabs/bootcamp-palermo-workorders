using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using System.ClientModel;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class WorkOrderSummarizeQueryHandlerTests
{
    [Test]
    public void BuildChatMessages_IncludesTitleDescriptionAndInstructions()
    {
        var query = new WorkOrderSummarizeQuery("Fix sink", "Leaking under cabinet", "Shut off water first");

        var messages = WorkOrderSummarizeQueryHandler.BuildChatMessages(query);

        messages.Count.ShouldBe(2);
        messages[0].Role.ShouldBe(ChatRole.System);
        messages[1].Role.ShouldBe(ChatRole.User);
        messages[1].Text.ShouldContain("Fix sink");
        messages[1].Text.ShouldContain("Leaking under cabinet");
        messages[1].Text.ShouldContain("Shut off water first");
    }

    [Test]
    public async Task Handle_WhenClientReturnsText_ReturnsSummary()
    {
        var factory = new StubChatClientFactory("One paragraph summary.");
        var handler = new WorkOrderSummarizeQueryHandler(
            factory, NullLogger<WorkOrderSummarizeQueryHandler>.Instance);
        var query = new WorkOrderSummarizeQuery("T", "D", "I");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Summary.ShouldBe("One paragraph summary.");
    }

    [Test]
    public async Task Handle_WhenClientUnavailable_ReturnsFriendlyMessage()
    {
        var factory = new UnavailableChatClientFactory();
        var handler = new WorkOrderSummarizeQueryHandler(
            factory, NullLogger<WorkOrderSummarizeQueryHandler>.Instance);
        var query = new WorkOrderSummarizeQuery("T", "D", "I");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Summary.ShouldBe(WorkOrderSummarizeQueryHandler.FriendlyUnavailableMessage);
    }

    [Test]
    public async Task Handle_WhenClientThrowsHttpRequestException_ReturnsFriendlyMessage()
    {
        var factory = new StubChatClientFactory(new HttpRequestException("down"));
        var handler = new WorkOrderSummarizeQueryHandler(
            factory, NullLogger<WorkOrderSummarizeQueryHandler>.Instance);
        var query = new WorkOrderSummarizeQuery("T", "D", "I");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Summary.ShouldBe(WorkOrderSummarizeQueryHandler.FriendlyUnavailableMessage);
    }

    [Test]
    public async Task Handle_WhenClientThrowsClientResultException_ReturnsFriendlyMessage()
    {
        var factory = new StubChatClientFactory(new ClientResultException("content_filter"));
        var handler = new WorkOrderSummarizeQueryHandler(
            factory, NullLogger<WorkOrderSummarizeQueryHandler>.Instance);
        var query = new WorkOrderSummarizeQuery("T", "D", "I");

        var result = await handler.Handle(query, CancellationToken.None);

        result.Summary.ShouldBe(WorkOrderSummarizeQueryHandler.FriendlyUnavailableMessage);
    }

    private sealed class UnavailableChatClientFactory() : ChatClientFactory(null!)
    {
        public override Task<IChatClient> GetChatClient() =>
            throw new InvalidOperationException("AI_OpenAI_ApiKey is not configured.");
    }

    private sealed class StubChatClientFactory : ChatClientFactory
    {
        private readonly IChatClient _client;

        public StubChatClientFactory(string responseText) : base(null!)
        {
            _client = new StubChatClient(responseText);
        }

        public StubChatClientFactory(Exception exception) : base(null!)
        {
            _client = new ThrowingChatClient(exception);
        }

        public override Task<IChatClient> GetChatClient() => Task.FromResult(_client);
    }

    private sealed class StubChatClient(string responseText) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ChatResponse([new ChatMessage(ChatRole.Assistant, responseText)]));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
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
}
