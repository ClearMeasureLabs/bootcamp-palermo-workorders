namespace ClearMeasure.Bootcamp.AgentProtocols;

/// <summary>
/// Channel an <see cref="IAcpAgentHandler"/> uses to push streaming updates back to the
/// ACP client and to request tool-call permission during a prompt turn.
/// </summary>
public interface IAcpAgentContext
{
    /// <summary>Sends an agent message chunk to the client for the given session.</summary>
    /// <param name="sessionId">Target session identifier.</param>
    /// <param name="text">Chunk of message text to append.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendMessageChunkAsync(string sessionId, string text, CancellationToken cancellationToken);

    /// <summary>Sends a tool call start or status update to the client.</summary>
    /// <param name="sessionId">Target session identifier.</param>
    /// <param name="toolCallId">Stable identifier of the tool call.</param>
    /// <param name="title">Human-readable description of the tool action.</param>
    /// <param name="kind">Category of the tool call.</param>
    /// <param name="status">Current lifecycle status of the tool call.</param>
    /// <param name="rawOutput">Optional raw output payload, attached to non-pending updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendToolCallAsync(string sessionId, string toolCallId, string title, AcpToolKind kind, AcpToolStatus status, string? rawOutput = null, CancellationToken cancellationToken = default);

    /// <summary>Asks the client to approve or deny a tool call and returns the decision.</summary>
    /// <param name="request">Details of the permission request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The client's decision.</returns>
    Task<AcpPermissionOutcome> RequestPermissionAsync(AcpPermissionRequest request, CancellationToken cancellationToken);
}
