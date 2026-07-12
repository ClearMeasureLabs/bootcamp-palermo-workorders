using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// Deterministic, offline <see cref="IChatClient"/> used when fake mode is enabled
/// (see <c>AI_OpenAI_UseFake</c>). It satisfies the text-only LLM paths — health-check
/// connectivity probes and translation — without calling Azure OpenAI, so those tests
/// run deterministically with no live external calls and no cost.
///
/// It deliberately does NOT attempt tool/function invocation or natural-language
/// reasoning: agentic scenarios (create/assign a work order from a prompt) are covered
/// by opt-in live tests, not by this fake.
/// </summary>
public sealed class FakeChatClient : IChatClient
{
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var reply = BuildReply(messages);
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var reply = BuildReply(messages);
        yield return new ChatResponseUpdate(ChatRole.Assistant, reply);
        await Task.CompletedTask;
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private static string BuildReply(IEnumerable<ChatMessage> messages)
    {
        var list = messages.ToList();
        var combined = string.Join(" ", list.Select(m => m.Text));

        // The health check sends "Reply with OK" and only needs a non-empty response.
        if (combined.Contains("Reply with OK", StringComparison.OrdinalIgnoreCase))
        {
            return "OK";
        }

        // Otherwise echo the last user message with a marker so callers (e.g. translation)
        // receive a non-empty result that differs from the original input.
        var lastUserText = list.LastOrDefault(m => m.Role == ChatRole.User)?.Text;
        return $"[fake-llm] {lastUserText}".Trim();
    }
}
