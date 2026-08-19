using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class TracingChatClientTests
{
    [Test]
    public async Task GetResponseAsync_WhenMessagesIsSingleUseEnumerable_ShouldReturnResponse()
    {
        var innerClient = new StubChatClient();
        var client = new TracingChatClient(innerClient);
        var messages = SingleUseMessages(new ChatMessage(ChatRole.User, "Fix the pipe"));

        var response = await client.GetResponseAsync(messages);

        response.Text.ShouldBe("stub-response");
        innerClient.ReceivedMessageCount.ShouldBe(1);
    }

    [Test]
    public async Task GetStreamingResponseAsync_WhenMessagesIsSingleUseEnumerable_ShouldYieldUpdates()
    {
        var innerClient = new StubChatClient();
        var client = new TracingChatClient(innerClient);
        var messages = SingleUseMessages(new ChatMessage(ChatRole.User, "Fix the pipe"));

        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(messages))
        {
            updates.Add(update);
        }

        updates.Count.ShouldBe(1);
        updates[0].Text.ShouldBe("stub-streaming");
    }

    private static IEnumerable<ChatMessage> SingleUseMessages(params ChatMessage[] messages)
    {
        var enumerated = false;
        return new SingleUseEnumerable(messages, () => enumerated ? throw new InvalidOperationException("Enumerated more than once.") : enumerated = true);
    }

    private class SingleUseEnumerable(ChatMessage[] messages, Func<bool> markEnumerated) : IEnumerable<ChatMessage>
    {
        public IEnumerator<ChatMessage> GetEnumerator()
        {
            markEnumerated();
            return ((IEnumerable<ChatMessage>)messages).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class StubChatClient : IChatClient
    {
        public int ReceivedMessageCount { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            ReceivedMessageCount = messages.Count();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "stub-response")));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ReceivedMessageCount = messages.Count();
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "stub-streaming");
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
