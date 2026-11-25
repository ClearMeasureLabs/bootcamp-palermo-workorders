using ClearMeasure.Bootcamp.AgentProtocols;

namespace ClearMeasure.Bootcamp.WorkOrderAgentSpike;

/// <summary>Configuration of a single HTTP MCP endpoint the spike agent can call.</summary>
/// <param name="Name">Human-readable name of the endpoint.</param>
/// <param name="Url">Base URL of the HTTP MCP server.</param>
/// <param name="Headers">Headers to forward with every request (for example an API key).</param>
/// <param name="Source">Where the endpoint was learned from, e.g. <c>session/new</c> or <c>env-fallback</c>.</param>
public sealed record McpEndpointConfig(string Name, string Url, IReadOnlyList<AcpMcpHttpHeader> Headers, string Source);

/// <summary>Abstraction over MCP tool invocations made by the spike agent.</summary>
public interface IMcpWorkOrderClient
{
    /// <summary>Invokes <paramref name="toolName"/> on the given endpoint and returns the textual tool output.</summary>
    /// <param name="endpoint">The HTTP MCP endpoint to call.</param>
    /// <param name="toolName">MCP tool to invoke.</param>
    /// <param name="arguments">Tool arguments (may contain null values).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Joined text content of the tool result.</returns>
    Task<string> CallToolAsync(McpEndpointConfig endpoint, string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken);
}
