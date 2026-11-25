using Acp;
using Acp.Schema;
using Acp.Streaming;

namespace ClearMeasure.Bootcamp.AgentProtocols.LibAcp;

/// <summary>
/// <see cref="IAcpAgentConnection"/> backed by the LibAcp 0.1.0 package.
/// All <c>Acp.*</c> types are confined to this file; the public surface only exposes the
/// <see cref="AgentProtocols"/> seam types.
/// </summary>
public sealed class LibAcpAgentConnection : IAcpAgentConnection
{
    private readonly AgentSideConnection _connection;

    // ReSharper disable once ConvertToPrimaryConstructor -- kept clean per convention
    private LibAcpAgentConnection(AgentSideConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc />
    public Task Closed => _connection.Closed;

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    /// <summary>Creates a connection that reads and writes JSON-RPC on the process stdio streams.</summary>
    /// <param name="handlerFactory">Factory invoked once with the wire connection to build the application-side handler.</param>
    /// <returns>The live ACP agent connection.</returns>
    public static IAcpAgentConnection CreateStdio(Func<IAcpAgentHandler> handlerFactory)
    {
        ArgumentNullException.ThrowIfNull(handlerFactory);
        var connection = new AgentSideConnection(conn => new AgentAdapter(conn, handlerFactory()), NdJsonStream.FromStdio());
        return new LibAcpAgentConnection(connection);
    }

    /// <summary>Creates a connection over the given in-memory or piped streams.</summary>
    /// <param name="input">Stream carrying client-to-agent JSON-RPC messages.</param>
    /// <param name="output">Stream carrying agent-to-client JSON-RPC messages.</param>
    /// <param name="handlerFactory">Factory invoked once with the wire connection to build the application-side handler.</param>
    /// <returns>The live ACP agent connection.</returns>
    public static IAcpAgentConnection CreateStream(Stream input, Stream output, Func<IAcpAgentHandler> handlerFactory)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(handlerFactory);
        var connection = new AgentSideConnection(conn => new AgentAdapter(conn, handlerFactory()), new NdJsonStream(input, output));
        return new LibAcpAgentConnection(connection);
    }

    /// <summary>Shared mapping helpers between the clean seam types and the LibAcp schema types.</summary>
    private static class Mappers
    {
        internal static ToolKind MapKind(AcpToolKind kind) => kind switch
        {
            AcpToolKind.Read => ToolKind.Read,
            AcpToolKind.Edit => ToolKind.Edit,
            AcpToolKind.Execute => ToolKind.Execute,
            AcpToolKind.Search => ToolKind.Search,
            _ => ToolKind.Other,
        };

        internal static ToolCallStatus MapStatus(AcpToolStatus status) => status switch
        {
            AcpToolStatus.Pending => ToolCallStatus.Pending,
            AcpToolStatus.InProgress => ToolCallStatus.InProgress,
            AcpToolStatus.Completed => ToolCallStatus.Completed,
            _ => ToolCallStatus.Failed,
        };

        internal static StopReason MapStopReason(AcpStopReason reason) => reason switch
        {
            AcpStopReason.EndTurn => StopReason.EndTurn,
            AcpStopReason.Cancelled => StopReason.Cancelled,
            AcpStopReason.Refusal => StopReason.Refusal,
            AcpStopReason.MaxTokens => StopReason.MaxTokens,
            _ => throw new ArgumentOutOfRangeException(nameof(reason)),
        };

        internal static IReadOnlyList<AcpMcpServerInfo> MapMcpServers(IReadOnlyList<McpServer>? servers)
            => servers is null ? [] : servers.Select(MapMcpServer).ToList();

        private static AcpMcpServerInfo MapMcpServer(McpServer server) => server switch
        {
            McpServerHttp http => new AcpMcpServerInfo(
                "http",
                http.Name,
                http.Url,
                MapHeaders(http.Headers)),
            McpServerSse sse => new AcpMcpServerInfo(
                "sse",
                sse.Name,
                sse.Url,
                MapHeaders(sse.Headers)),
            McpServerStdio stdio => new AcpMcpServerInfo(
                "stdio",
                stdio.Name,
                null,
                [],
                stdio.Command,
                stdio.Args?.ToList()),
            _ => new AcpMcpServerInfo("unknown", string.Empty, null, []),
        };

        private static IReadOnlyList<AcpMcpHttpHeader> MapHeaders(IReadOnlyList<HttpHeader>? headers)
            => headers is null ? [] : headers.Select(h => new AcpMcpHttpHeader(h.Name, h.Value)).ToList();
    }

    /// <summary>Implements the LibAcp agent interface by delegating to the clean <see cref="IAcpAgentHandler"/>.</summary>
    private sealed class AgentAdapter(AgentSideConnection connection, IAcpAgentHandler handler) : IAgent
    {
        public async Task<InitializeResponse> InitializeAsync(InitializeRequest request, CancellationToken cancellationToken)
        {
            var result = await handler.InitializeAsync(
                new AcpInitializeRequest(request.ProtocolVersion, request.ClientInfo?.Name, request.ClientInfo?.Version),
                cancellationToken).ConfigureAwait(false);

            return new InitializeResponse
            {
                ProtocolVersion = result.ProtocolVersion,
                AgentInfo = new Implementation
                {
                    Name = result.AgentInfo.Name,
                    Version = result.AgentInfo.Version,
                    Title = result.AgentInfo.Title,
                },
                AgentCapabilities = new AgentCapabilities
                {
                    LoadSession = result.Capabilities.LoadSession,
                    McpCapabilities = new McpCapabilities
                    {
                        Http = result.Capabilities.McpCapabilities.Http,
                        Sse = result.Capabilities.McpCapabilities.Sse,
                    },
                },
            };
        }

        public Task<AuthenticateResponse?> AuthenticateAsync(AuthenticateRequest request, CancellationToken cancellationToken)
            => Task.FromResult<AuthenticateResponse?>(new AuthenticateResponse());

        public async Task<NewSessionResponse> NewSessionAsync(NewSessionRequest request, CancellationToken cancellationToken)
        {
            var result = await handler.NewSessionAsync(
                new AcpNewSessionRequest(request.Cwd, Mappers.MapMcpServers(request.McpServers)),
                cancellationToken).ConfigureAwait(false);

            return new NewSessionResponse { SessionId = new SessionId(result.SessionId) };
        }

        public async Task<PromptResponse> PromptAsync(PromptRequest request, CancellationToken cancellationToken)
        {
            var promptText = string.Join("\n", request.Prompt.OfType<TextContent>().Select(t => t.Text));
            var result = await handler.PromptAsync(
                new AcpPromptRequest(request.SessionId.Value, promptText),
                new ContextAdapter(connection),
                cancellationToken).ConfigureAwait(false);

            return new PromptResponse { StopReason = Mappers.MapStopReason(result.StopReason) };
        }

        public Task CancelAsync(CancelNotification notification, CancellationToken cancellationToken)
            => handler.CancelAsync(notification.SessionId.Value, cancellationToken);
    }

    /// <summary>Implements the clean <see cref="IAcpAgentContext"/> on top of the LibAcp wire connection.</summary>
    private sealed class ContextAdapter(AgentSideConnection connection) : IAcpAgentContext
    {
        public Task SendMessageChunkAsync(string sessionId, string text, CancellationToken cancellationToken)
            => connection.SessionUpdateAsync(new SessionNotification
            {
                SessionId = new SessionId(sessionId),
                Update = new AgentMessageChunk { Content = new TextContent { Text = text } },
            }, cancellationToken);

        public Task SendToolCallAsync(
            string sessionId,
            string toolCallId,
            string title,
            AcpToolKind kind,
            AcpToolStatus status,
            string? rawOutput,
            CancellationToken cancellationToken)
            => connection.SessionUpdateAsync(BuildToolUpdate(sessionId, toolCallId, title, kind, status, rawOutput), cancellationToken);

        public async Task<AcpPermissionOutcome> RequestPermissionAsync(AcpPermissionRequest request, CancellationToken cancellationToken)
        {
            var response = await connection.RequestPermissionAsync(MapPermissionRequest(request), cancellationToken).ConfigureAwait(false);
            return MapPermissionOutcome(response.Outcome);
        }

        private static SessionNotification BuildToolUpdate(
            string sessionId,
            string toolCallId,
            string title,
            AcpToolKind kind,
            AcpToolStatus status,
            string? rawOutput)
            => new()
            {
                SessionId = new SessionId(sessionId),
                Update = status == AcpToolStatus.Pending
                    ? new ToolCallStartUpdate
                    {
                        ToolCallId = new ToolCallId(toolCallId),
                        Title = title,
                        Kind = Mappers.MapKind(kind),
                        Status = Mappers.MapStatus(status),
                    }
                    : new ToolCallUpdateUpdate
                    {
                        ToolCallId = new ToolCallId(toolCallId),
                        Title = title,
                        Kind = Mappers.MapKind(kind),
                        Status = Mappers.MapStatus(status),
                        Content = rawOutput is null
                            ? null
                            : [new ToolCallContentBlock { Content = new TextContent { Text = rawOutput } }],
                    },
            };

        private static RequestPermissionRequest MapPermissionRequest(AcpPermissionRequest request) => new()
        {
            SessionId = new SessionId(request.SessionId),
            ToolCall = new ToolCallUpdate
            {
                ToolCallId = new ToolCallId(request.ToolCallId),
                Title = request.Title,
                Kind = Mappers.MapKind(request.Kind),
                Status = ToolCallStatus.Pending,
            },
            Options = request.Options
                .Select(o => new PermissionOption
                {
                    OptionId = new PermissionOptionId(o.OptionId),
                    Name = o.Name,
                    Kind = o.IsAllow ? PermissionOptionKind.AllowOnce : PermissionOptionKind.RejectOnce,
                })
                .ToArray(),
        };

        private static AcpPermissionOutcome MapPermissionOutcome(RequestPermissionOutcome outcome) => outcome switch
        {
            SelectedPermissionOutcome selected => new AcpPermissionOutcome(false, selected.OptionId.Value),
            _ => new AcpPermissionOutcome(true, null),
        };
    }
}
