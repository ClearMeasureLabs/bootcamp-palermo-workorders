namespace ClearMeasure.Bootcamp.AgentProtocols;

/// <summary>
/// Application-side implementation of the ACP agent half of the protocol.
/// Implementations are invoked by the connection adapter for each inbound protocol message.
/// </summary>
// ReSharper disable UnusedParameter.Global -- interface contract requires CancellationToken for async implementations
public interface IAcpAgentHandler
{
    /// <summary>Handles an initialization request from the ACP client.</summary>
    /// <param name="request">The client's initialization details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent's negotiated protocol version, identity and capabilities.</returns>
    Task<AcpInitializeResult> InitializeAsync(AcpInitializeRequest request, CancellationToken cancellationToken);

    /// <summary>Creates a new ACP session.</summary>
    /// <param name="request">Session creation details, including MCP servers to attach.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The identifier of the created session.</returns>
    Task<AcpNewSessionResult> NewSessionAsync(AcpNewSessionRequest request, CancellationToken cancellationToken);

    /// <summary>Runs one prompt turn in an existing session.</summary>
    /// <param name="request">The session and prompt text.</param>
    /// <param name="context">Channel for sending streaming updates, tool calls and permission requests back to the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stop reason for the completed turn.</returns>
    Task<AcpPromptResult> PromptAsync(AcpPromptRequest request, IAcpAgentContext context, CancellationToken cancellationToken);

    /// <summary>Cancels the in-flight turn of a session, if any.</summary>
    /// <param name="sessionId">Session to cancel.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CancelAsync(string sessionId, CancellationToken cancellationToken);
}
// ReSharper restore UnusedParameter.Global
