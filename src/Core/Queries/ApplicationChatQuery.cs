using MediatR;

namespace ClearMeasure.Bootcamp.Core.Queries;

/// <summary>
/// Remotable application-chat turn. Returns a plain DTO so Blazor WASM remoting
/// does not deserialize <c>Microsoft.Extensions.AI.ChatResponse</c> (TypeLoadException).
/// </summary>
public record ApplicationChatQuery(string Prompt, string CurrentUsername)
    : IRequest<ApplicationChatResult>, IRemotableRequest
{
    /// <summary>
    /// Prior messages supplied as context for this chat turn.
    /// </summary>
    public List<ChatHistoryMessage> ChatHistory { get; init; } = [];
}

/// <summary>
/// Serializable chat reply for the WASM remoting boundary (lives in Core — no AI package deps).
/// </summary>
public record ApplicationChatResult(string Text);

/// <summary>
/// Serializable chat-history entry for the WASM remoting boundary.
/// </summary>
public record ChatHistoryMessage(string Role, string Content);
