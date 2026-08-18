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
        var innerClient = new StubChatClient();
        var client = new TracingChatClient(innerClient);
        var messages = new SingleUseMessageSequence(new ChatMessage(ChatRole.User, "Fix the pipe"));

        var response = await client.GetResponseAsync(messages);

        response.ShouldNotBeNull();
        messages.EnumerationCount.ShouldBe(1);
    }

    [Test]
    public async Task ShouldEnumerateMessagesExactlyOnce_When_GetStreamingResponseAsync()
    {
        var innerClient = new StubChatClient();
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

    /// <summary>
    /// A conversation history that can be enumerated exactly once, mimicking a deferred, one-shot
    /// source such as a database-backed LINQ query. A second <see cref="GetEnumerator"/> call throws,
    /// proving the caller materialized the sequence instead of enumerating it twice.
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

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private class StubChatClient : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Fixed"))
            {
                ModelId = "stub-model"
            };
            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new ChatResponseUpdate(ChatRole.Assistant, "Fixed") { ModelId = "stub-model" };
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}
