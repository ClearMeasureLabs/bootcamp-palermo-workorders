namespace ClearMeasure.Bootcamp.AgentProtocols;

/// <summary>
/// A live ACP agent connection. Transport-specific details (stdio, in-memory streams, sockets)
/// are hidden behind this seam; consumers only observe the <see cref="Closed"/> signal and
/// dispose the connection when done.
/// </summary>
public interface IAcpAgentConnection : IAsyncDisposable
{
    /// <summary>Gets a task that completes when the connection is closed by either side.</summary>
    Task Closed { get; }
}
