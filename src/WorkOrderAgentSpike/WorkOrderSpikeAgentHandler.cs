using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.AgentProtocols;

namespace ClearMeasure.Bootcamp.WorkOrderAgentSpike;

/// <summary>Minimal ACP v1 spike agent that routes prompts to Church Bulletin work-order MCP tools over HTTP.</summary>
public sealed class WorkOrderSpikeAgentHandler(IMcpWorkOrderClient mcpClient) : IAcpAgentHandler
{
    /// <summary>Client used to invoke work-order MCP tools.</summary>
    private readonly ConcurrentDictionary<string, McpEndpointConfig?> _endpoints = new();

    /// <summary>Responds to the ACP initialize request with the spike agent's identity and capabilities.</summary>
    public Task<AcpInitializeResult> InitializeAsync(AcpInitializeRequest request, CancellationToken cancellationToken)
    {
        LogClientInfo(request);
        return Task.FromResult(new AcpInitializeResult(
            AcpProtocol.Version,
            new AcpAgentInfo("churchbulletin-workorder-spike", "0.1.0", "Church Portal Spike Agent"),
            new AcpAgentCapabilities(false, new AcpMcpCapabilities(true, false))));
    }

    /// <summary>Creates a session and remembers which HTTP MCP endpoint it should use.</summary>
    public Task<AcpNewSessionResult> NewSessionAsync(AcpNewSessionRequest request, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid().ToString("n");
        _endpoints[sessionId] = ResolveEndpoint(request.McpServers);
        LogSessionServers(request.McpServers);
        return Task.FromResult(new AcpNewSessionResult(sessionId));
    }

    /// <summary>Runs one prompt turn, dispatching on the spike intent of the prompt text.</summary>
    public Task<AcpPromptResult> PromptAsync(AcpPromptRequest request, IAcpAgentContext context, CancellationToken cancellationToken)
    {
        var intent = Classify(request.PromptText);
        if (intent == SpikeIntent.ListWorkOrders) return ListWorkOrdersAsync(request, context, cancellationToken);
        if (intent == SpikeIntent.CreateWorkOrder) return CreateWorkOrderAsync(request, context, cancellationToken);
        if (intent == SpikeIntent.ExecuteCommand) return ExecuteCommandAsync(request, context, cancellationToken);
        return GreetingAsync(request, context, cancellationToken);
    }

    /// <summary>Cancels the in-flight turn of the session.</summary>
    public Task CancelAsync(string sessionId, CancellationToken cancellationToken)
    {
        Console.Error.WriteLine($"acp cancel session={sessionId}");
        return Task.CompletedTask;
    }

    /// <summary>Runs the read-only <c>list-work-orders</c> MCP tool and streams the result.</summary>
    private async Task<AcpPromptResult> ListWorkOrdersAsync(AcpPromptRequest request, IAcpAgentContext context, CancellationToken cancellationToken)
    {
        var toolCallId = NewToolCallId();
        await context.SendToolCallAsync(request.SessionId, toolCallId, "list-work-orders", AcpToolKind.Read, AcpToolStatus.Pending, null, cancellationToken);
        var endpoint = EndpointFor(request.SessionId);
        var output = endpoint is null
            ? "No MCP endpoint configured."
            : await mcpClient.CallToolAsync(endpoint, "list-work-orders", new Dictionary<string, object?>(), cancellationToken);
        await context.SendToolCallAsync(request.SessionId, toolCallId, "list-work-orders", AcpToolKind.Read, AcpToolStatus.Completed, output, cancellationToken);
        await context.SendMessageChunkAsync(request.SessionId, FormatListResponse(endpoint, output), cancellationToken);
        return new AcpPromptResult(AcpStopReason.EndTurn);
    }

    /// <summary>Runs the mutating <c>create-work-order</c> MCP tool after a permission check.</summary>
    private Task<AcpPromptResult> CreateWorkOrderAsync(AcpPromptRequest request, IAcpAgentContext context, CancellationToken cancellationToken)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["title"] = ExtractTitle(request.PromptText),
            ["description"] = "Created via ACP spike",
            ["creatorUsername"] = Environment.GetEnvironmentVariable("CHURCHBULLETIN_USERNAME") ?? "jpalermo",
        };
        return ExecuteMutationAsync(request, context, "create-work-order", AcpToolKind.Edit, arguments, cancellationToken);
    }

    /// <summary>Runs the <c>execute-work-order-command</c> MCP tool after a permission check.</summary>
    private Task<AcpPromptResult> ExecuteCommandAsync(AcpPromptRequest request, IAcpAgentContext context, CancellationToken cancellationToken)
    {
        var command = request.PromptText.Trim();
        var commandName = ResolveCommandName(command);
        var arguments = new Dictionary<string, object?>
        {
            ["workOrderNumber"] = ExtractWorkOrderNumber(command),
            ["commandName"] = commandName,
            ["executingUsername"] = Environment.GetEnvironmentVariable("CHURCHBULLETIN_USERNAME") ?? "jpalermo",
        };
        if (commandName == "DraftToAssignedCommand")
            arguments["assigneeUsername"] = Environment.GetEnvironmentVariable("CHURCHBULLETIN_USERNAME") ?? "jpalermo";
        return ExecuteMutationAsync(request, context, "execute-work-order-command", AcpToolKind.Execute, arguments, cancellationToken);
    }

    /// <summary>Maps the spike command keyword to the concrete Church Bulletin command name.</summary>
    private static string ResolveCommandName(string prompt)
    {
        var p = prompt.ToLowerInvariant();
        if (p.Contains("complete")) return "InProgressToCompleteCommand";
        if (p.Contains("assign")) return "DraftToAssignedCommand";
        if (p.Contains("cancel")) return "AssignedToCancelledCommand";
        return "AssignedToInProgressCommand";
    }

    /// <summary>Shared permission-gated path for mutating tool calls.</summary>
    private async Task<AcpPromptResult> ExecuteMutationAsync(AcpPromptRequest request, IAcpAgentContext context, string toolName, AcpToolKind kind, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var toolCallId = NewToolCallId();
        await context.SendToolCallAsync(request.SessionId, toolCallId, toolName, kind, AcpToolStatus.Pending, null, cancellationToken);
        var outcome = await context.RequestPermissionAsync(BuildPermissionRequest(request.SessionId, toolCallId, toolName, kind), cancellationToken);
        if (outcome.Cancelled || outcome.SelectedOptionId != "allow_once")
            return await SendFailureAsync(request, context, toolCallId, toolName, kind, "Permission declined; no work order change was executed.", outcome.Cancelled ? AcpStopReason.Cancelled : AcpStopReason.EndTurn, cancellationToken);
        var endpoint = EndpointFor(request.SessionId);
        if (endpoint is null)
            return await SendFailureAsync(request, context, toolCallId, toolName, kind, "No MCP endpoint configured.", AcpStopReason.Refusal, cancellationToken);
        var output = await mcpClient.CallToolAsync(endpoint, toolName, arguments, cancellationToken);
        await context.SendToolCallAsync(request.SessionId, toolCallId, toolName, kind, AcpToolStatus.Completed, output, cancellationToken);
        await context.SendMessageChunkAsync(request.SessionId, output, cancellationToken);
        return new AcpPromptResult(AcpStopReason.EndTurn);
    }

    /// <summary>Streams a greeting describing the spike agent and the session's MCP endpoint summary.</summary>
    private async Task<AcpPromptResult> GreetingAsync(AcpPromptRequest request, IAcpAgentContext context, CancellationToken cancellationToken)
    {
        var text = $"Hello! I am the churchbulletin-workorder-spike ACP v1 LibAcp stdio agent. session/new MCP endpoint: {Summarize(EndpointFor(request.SessionId))}.";
        await context.SendMessageChunkAsync(request.SessionId, text, cancellationToken);
        return new AcpPromptResult(AcpStopReason.EndTurn);
    }

    /// <summary>Sends a failed tool call and a decline message for the given stop reason.</summary>
    private async Task<AcpPromptResult> SendFailureAsync(AcpPromptRequest request, IAcpAgentContext context, string toolCallId, string toolName, AcpToolKind kind, string message, AcpStopReason stopReason, CancellationToken cancellationToken)
    {
        await context.SendToolCallAsync(request.SessionId, toolCallId, toolName, kind, AcpToolStatus.Failed, message, cancellationToken);
        await context.SendMessageChunkAsync(request.SessionId, message, cancellationToken);
        return new AcpPromptResult(stopReason);
    }

    /// <summary>Builds the permission request offering allow-once and reject options.</summary>
    private static AcpPermissionRequest BuildPermissionRequest(string sessionId, string toolCallId, string toolName, AcpToolKind kind)
    {
        var options = new[]
        {
            new AcpPermissionOption("allow_once", "Allow once", true),
            new AcpPermissionOption("reject_once", "Reject", false),
        };
        return new AcpPermissionRequest(sessionId, toolCallId, $"Approve {toolName}", kind, options);
    }

    /// <summary>Formats the streamed list response: MCP source summary plus the raw tool output.</summary>
    private static string FormatListResponse(McpEndpointConfig? endpoint, string output)
        => $"MCP source: {Summarize(endpoint)}\n{output}";

    /// <summary>Summarizes an endpoint as name/url/header names/source, or <c>none configured</c>.</summary>
    private static string Summarize(McpEndpointConfig? endpoint)
    {
        if (endpoint is null) return "none configured";
        var headerNames = string.Join(", ", endpoint.Headers.Select(h => h.Name));
        return $"Name={endpoint.Name} Url={endpoint.Url} headers=[{headerNames}] Source={endpoint.Source}";
    }

    /// <summary>Classifies a prompt into one of the spike intents.</summary>
    private static SpikeIntent Classify(string prompt)
    {
        var p = prompt.ToLowerInvariant();
        if (!HasAnyWord(p, MutationWords)) return HasListWords(p) ? SpikeIntent.ListWorkOrders : SpikeIntent.Greeting;
        return HasAnyWord(p, ExecuteWords) ? SpikeIntent.ExecuteCommand : SpikeIntent.CreateWorkOrder;
    }

    /// <summary>True when the prompt contains one of the mutation keywords as a whole word.</summary>
    private static bool HasAnyWord(string text, string[] words)
    {
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return tokens.Any(w => words.Contains(w.Trim(',', '.', ';', ':', '!', '?'), StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>True when the prompt mentions listing work orders.</summary>
    private static bool HasListWords(string p)
        => HasAnyWord(p, ["list", "assigned"]) || p.Contains("work order");

    /// <summary>Keywords that make a prompt mutating.</summary>
    private static readonly string[] MutationWords = ["create", "complete", "assign", "cancel", "change", "update"];

    /// <summary>Keywords that select the execute-command tool over create.</summary>
    private static readonly string[] ExecuteWords = ["complete", "assign", "cancel"];

    /// <summary>Picks the session endpoint from the MCP servers, falling back to environment variables.</summary>
    private static McpEndpointConfig? ResolveEndpoint(IReadOnlyList<AcpMcpServerInfo> servers)
    {
        foreach (var server in servers)
        {
            if (IsHttpServer(server))
                return new McpEndpointConfig(server.Name, server.Url!, server.Headers, "session/new");
        }
        return EnvFallbackEndpoint();
    }

    /// <summary>True when the server entry is an HTTP transport with a non-empty URL.</summary>
    private static bool IsHttpServer(AcpMcpServerInfo server)
        => string.Equals(server.TransportType, "http", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(server.Url);

    /// <summary>Builds an endpoint from <c>CHURCHBULLETIN_MCP_URL</c> (and optional <c>CHURCHBULLETIN_API_KEY</c>).</summary>
    private static McpEndpointConfig? EnvFallbackEndpoint()
    {
        var url = Environment.GetEnvironmentVariable("CHURCHBULLETIN_MCP_URL");
        if (string.IsNullOrEmpty(url)) return null;
        var headers = new List<AcpMcpHttpHeader>();
        var apiKey = Environment.GetEnvironmentVariable("CHURCHBULLETIN_API_KEY");
        if (!string.IsNullOrEmpty(apiKey)) headers.Add(new AcpMcpHttpHeader("X-API-Key", apiKey));
        return new McpEndpointConfig("env-mcp", url, headers, "env-fallback");
    }

    /// <summary>Logs session servers to stderr: count, name, transport, URL, header names and lengths (never values).</summary>
    private static void LogSessionServers(IReadOnlyList<AcpMcpServerInfo> servers)
    {
        Console.Error.WriteLine($"session/new: {servers.Count} MCP server(s)");
        foreach (var server in servers)
        {
            Console.Error.WriteLine($"  server Name={server.Name} TransportType={server.TransportType} Url={server.Url ?? "(none)"}");
            foreach (var header in server.Headers)
                Console.Error.WriteLine($"    header {header.Name} len={header.Value.Length}");
        }
    }

    /// <summary>Logs the initializing client's identity to stderr.</summary>
    private static void LogClientInfo(AcpInitializeRequest request)
        => Console.Error.WriteLine($"acp initialize protocol={request.ProtocolVersion} client={request.ClientName ?? "-"} clientVersion={request.ClientVersion ?? "-"}");

    /// <summary>Extracts the title from the prompt, stripping a leading create-work-order prefix (falls back to a default).</summary>
    private static string ExtractTitle(string prompt)
    {
        var trimmed = prompt.Trim();
        if (trimmed.StartsWith("create work order", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed["create work order".Length..].TrimStart();
        else if (trimmed.StartsWith("create", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed["create".Length..].TrimStart();
        return string.IsNullOrEmpty(trimmed) ? "Work order" : trimmed;
    }

    /// <summary>Extracts a <c>WO-123</c>-style work order number from the command text.</summary>
    private static string ExtractWorkOrderNumber(string command)
    {
        var match = Regex.Match(command, @"\b(?:WO-\d+|[A-F0-9]{5,8})\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value : "unknown";
    }

    /// <summary>Returns the MCP endpoint stored for the session, if any.</summary>
    private McpEndpointConfig? EndpointFor(string sessionId)
        => _endpoints.GetValueOrDefault(sessionId);

    /// <summary>Generates a fresh tool call identifier.</summary>
    private static string NewToolCallId() => Guid.NewGuid().ToString("n");

    /// <summary>Spike intents recognized by <see cref="Classify"/>.</summary>
    private enum SpikeIntent
    {
        Greeting,
        ListWorkOrders,
        CreateWorkOrder,
        ExecuteCommand,
    }
}
