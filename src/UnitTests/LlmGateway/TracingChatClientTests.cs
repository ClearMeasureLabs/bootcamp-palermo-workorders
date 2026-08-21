using System.Runtime.CompilerServices;
using ClearMeasure.Bootcamp.LlmGateway;
using Microsoft.Extensions.AI;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.LlmGateway;

[TestFixture]
public class TracingChatClientTests
{
    [Test]
    public async Task ShouldEnumerateMessagesExactlyOnce_When_GetResponseAsync()
    {
        var innerClient = new ConsumingStubChatClient();
        var client = new TracingChatClient(innerClient);
        var messages = new SingleUseMessageSequence(new ChatMessage(ChatRole.User, "Fix the pipe"));

        var response = await client.GetResponseAsync(messages);

        response.ShouldNotBeNull();
        messages.EnumerationCount.ShouldBe(1);
    }

    [Test]
    public async Task ShouldEnumerateMessagesExactlyOnce_When_GetStreamingResponseAsync()
    {
        var innerClient = new ConsumingStubChatClient();
        var client = new TracingChatClient(innerClient);
        var messages = new SingleUseMessageSequence(new ChatMessage(ChatRole.User, "Fix the pipe"));

        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(messages))
        {
            updates.Add(update);
        }

        updates.ShouldNotBeEmpty();
        messages.EnumerationCount.ShouldBe(1);
    }

    [Test]
    public async Task GetStreamingResponseAsync_ShouldYieldTextUpdates()
    {
        var inner = new StubChatClient(
        [
            new ChatResponseUpdate(ChatRole.Assistant, "  "),
            new ChatResponseUpdate(ChatRole.Assistant, "Hello") { ModelId = "test-model" }
        ]);
        var client = new TracingChatClient(inner);
        var messages = new[] { new ChatMessage(ChatRole.User, "prompt") };

        var updates = new List<ChatResponseUpdate>();
        await foreach (var update in client.GetStreamingResponseAsync(messages))
        {
            updates.Add(update);
        }

        updates.Count.ShouldBe(1);
        updates[0].Text.ShouldBe("Hello");
    }

    [Test]
    public async Task GetStreamingResponseAsync_ShouldRethrow_WhenInnerEnumeratorFails()
    {
        var inner = new StubChatClient(failOnMoveNext: true);
        var client = new TracingChatClient(inner);
        var messages = new[] { new ChatMessage(ChatRole.User, "prompt") };

        var act = async () =>
        {
            await foreach (var _ in client.GetStreamingResponseAsync(messages))
            {
            }
        };

        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task GetResponseAsync_ShouldReturnInnerResponse()
    {
        var expected = new ChatResponse([new ChatMessage(ChatRole.Assistant, "ok")]) { ModelId = "m1" };
        var inner = new StubChatClient(response: expected);
        var client = new TracingChatClient(inner);

        var result = await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]);

        result.ModelId.ShouldBe("m1");
        result.Text.ShouldBe("ok");
    }

    [Test]
    public async Task GetResponseAsync_ShouldRethrow_WhenInnerThrows()
    {
        var inner = new StubChatClient(failOnGetResponse: true);
        var client = new TracingChatClient(inner);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await client.GetResponseAsync([new ChatMessage(ChatRole.User, "hi")]));
    }

    /// <summary>
    /// Conversation history that can be enumerated exactly once (guards PossibleMultipleEnumeration).
    /// </summary>
    private class SingleUseMessageSequence(params ChatMessage[] messages) : IEnumerable<ChatMessage>
    {
        public int EnumerationCount { get; private set; }

        public IEnumerator<ChatMessage> GetEnumerator()
        {
            EnumerationCount++;
            if (EnumerationCount > 1)
            {
                throw new InvalidOperationException("This sequence can only be enumerated once.");
            }

            return messages.AsEnumerable().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class ConsumingStubChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            _ = messages.ToList();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Fixed"))
            {
                ModelId = "stub-model"
            });
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _ = messages.ToList();
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "Fixed") { ModelId = "stub-model" };
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class StubChatClient(
        IReadOnlyList<ChatResponseUpdate>? updates = null,
        ChatResponse? response = null,
        bool failOnMoveNext = false,
        bool failOnGetResponse = false) : IChatClient
    {
        public void Dispose()
        {
        }

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (failOnGetResponse)
            {
                throw new InvalidOperationException("get-response-failed");
            }

            return Task.FromResult(response ?? new ChatResponse([]));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (failOnMoveNext)
            {
                throw new InvalidOperationException("stream-failed");
            }

            foreach (var update in updates ?? [])
            {
                yield return update;
            }

            await Task.CompletedTask;
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
    }
}
