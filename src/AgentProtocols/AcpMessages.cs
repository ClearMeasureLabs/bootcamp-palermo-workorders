// ReSharper disable NotAccessedPositionalProperty.Global -- protocol DTOs: some fields (e.g. AcpPermissionOutcome, client metadata) are consumed by future callers across assembly boundaries
namespace ClearMeasure.Bootcamp.AgentProtocols;

/// <summary>Protocol-level constants for the Agent Client Protocol (ACP) seam.</summary>
public static class AcpProtocol
{
    /// <summary>The ACP protocol version implemented by this seam (v1).</summary>
    public const int Version = 1;
}

/// <summary>Identity information about the ACP agent, as reported during initialization.</summary>
/// <param name="Name">Stable machine-readable name of the agent.</param>
/// <param name="Version">Version string of the agent implementation.</param>
/// <param name="Title">Optional human-readable title for display.</param>
public sealed record AcpAgentInfo(string Name, string Version, string? Title = null);

/// <summary>Indicates which MCP transport capabilities the agent supports.</summary>
/// <param name="Http">Whether the agent supports HTTP-based MCP servers.</param>
/// <param name="Sse">Whether the agent supports SSE-based MCP servers.</param>
public sealed record AcpMcpCapabilities(bool Http, bool Sse);

/// <summary>Capabilities reported by the agent during initialization.</summary>
/// <param name="LoadSession">Whether the agent can load an existing session.</param>
/// <param name="McpCapabilities">Supported MCP transport capabilities.</param>
public sealed record AcpAgentCapabilities(bool LoadSession, AcpMcpCapabilities McpCapabilities);

/// <summary>Client-side initialization request sent to the agent.</summary>
/// <param name="ProtocolVersion">Protocol version the client speaks.</param>
/// <param name="ClientName">Optional name of the client application.</param>
/// <param name="ClientVersion">Optional version of the client application.</param>
public sealed record AcpInitializeRequest(int ProtocolVersion, string? ClientName, string? ClientVersion);

/// <summary>Agent-side response to an <see cref="AcpInitializeRequest"/>.</summary>
/// <param name="ProtocolVersion">Protocol version negotiated with the client.</param>
/// <param name="AgentInfo">Identity of the agent.</param>
/// <param name="Capabilities">Capabilities supported by the agent.</param>
public sealed record AcpInitializeResult(int ProtocolVersion, AcpAgentInfo AgentInfo, AcpAgentCapabilities Capabilities);

/// <summary>An HTTP header to forward to an HTTP-based MCP server.</summary>
/// <param name="Name">Header name.</param>
/// <param name="Value">Header value.</param>
public sealed record AcpMcpHttpHeader(string Name, string Value);

/// <summary>Description of an MCP server to attach to a new ACP session.</summary>
/// <param name="TransportType">Transport discriminator, for example <c>http</c>, <c>sse</c> or <c>stdio</c>.</param>
/// <param name="Name">Name of the MCP server.</param>
/// <param name="Url">Endpoint URL for remote transports.</param>
/// <param name="Headers">Headers to send for remote transports.</param>
/// <param name="Command">Command to launch for stdio transports.</param>
/// <param name="Args">Arguments for the stdio command.</param>
public sealed record AcpMcpServerInfo(string TransportType, string Name, string? Url, IReadOnlyList<AcpMcpHttpHeader> Headers, string? Command = null, IReadOnlyList<string>? Args = null);

/// <summary>Request to create a new ACP session.</summary>
/// <param name="Cwd">Working directory for the session.</param>
/// <param name="McpServers">MCP servers to attach to the session.</param>
public sealed record AcpNewSessionRequest(string Cwd, IReadOnlyList<AcpMcpServerInfo> McpServers);

/// <summary>Result of an <see cref="AcpNewSessionRequest"/>.</summary>
/// <param name="SessionId">Identifier of the created session.</param>
public sealed record AcpNewSessionResult(string SessionId);

/// <summary>Request to run a prompt turn in an ACP session.</summary>
/// <param name="SessionId">Target session identifier.</param>
/// <param name="PromptText">Full prompt text to send to the agent.</param>
public sealed record AcpPromptRequest(string SessionId, string PromptText);

/// <summary>Reason why a prompt turn stopped.</summary>
public enum AcpStopReason
{
    /// <summary>The agent finished the turn normally.</summary>
    EndTurn,

    /// <summary>The turn was cancelled by the client.</summary>
    Cancelled,

    /// <summary>The agent refused to continue.</summary>
    Refusal,

    /// <summary>The turn stopped because the token budget was exhausted.</summary>
    MaxTokens,
}

/// <summary>Result of an <see cref="AcpPromptRequest"/>.</summary>
/// <param name="StopReason">Reason the prompt turn ended.</param>
public sealed record AcpPromptResult(AcpStopReason StopReason);

/// <summary>Category of a tool invocation, used for display and permission decisions.</summary>
public enum AcpToolKind
{
    /// <summary>Read-only access.</summary>
    Read,

    /// <summary>Mutating edit of files or state.</summary>
    Edit,

    /// <summary>Execution of a command or code.</summary>
    Execute,

    /// <summary>Search or lookup.</summary>
    Search,

    /// <summary>Anything that does not fit the other categories.</summary>
    Other,
}

/// <summary>Lifecycle status of a tool invocation.</summary>
public enum AcpToolStatus
{
    /// <summary>The tool call was announced but has not started.</summary>
    Pending,

    /// <summary>The tool call is currently running.</summary>
    InProgress,

    /// <summary>The tool call finished successfully.</summary>
    Completed,

    /// <summary>The tool call failed.</summary>
    Failed,
}

/// <summary>A choice the client can grant or deny in a permission request.</summary>
/// <param name="OptionId">Stable identifier of the option.</param>
/// <param name="Name">Human-readable name of the option.</param>
/// <param name="IsAllow">Whether selecting this option grants permission.</param>
public sealed record AcpPermissionOption(string OptionId, string Name, bool IsAllow);

/// <summary>Request from the agent asking the client to approve a tool call.</summary>
/// <param name="SessionId">Session the tool call belongs to.</param>
/// <param name="ToolCallId">Identifier of the tool call awaiting permission.</param>
/// <param name="Title">Human-readable title describing the action.</param>
/// <param name="Kind">Category of the tool call.</param>
/// <param name="Options">Choices offered to the client.</param>
public sealed record AcpPermissionRequest(string SessionId, string ToolCallId, string Title, AcpToolKind Kind, IReadOnlyList<AcpPermissionOption> Options);

/// <summary>Client's decision in response to an <see cref="AcpPermissionRequest"/>.</summary>
/// <param name="Cancelled">Whether the client dismissed the request without choosing.</param>
/// <param name="SelectedOptionId">Identifier of the chosen option, if any.</param>
public sealed record AcpPermissionOutcome(bool Cancelled, string? SelectedOptionId);
// ReSharper restore NotAccessedPositionalProperty.Global
