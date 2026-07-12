using Microsoft.Extensions.AI;

namespace ClearMeasure.Bootcamp.LlmGateway;

/// <summary>
/// Abstraction over chat-client creation so consumers depend on a seam rather than the
/// concrete Azure OpenAI construction. Enables a deterministic fake client to be
/// substituted in tests (see <see cref="FakeChatClient"/>).
/// </summary>
public interface IChatClientFactory
{
    Task<ChatClientAvailabilityResult> IsChatClientAvailable();
    Task<IChatClient> GetChatClient();
}
