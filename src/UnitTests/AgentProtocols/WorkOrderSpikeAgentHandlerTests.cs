using ClearMeasure.Bootcamp.AgentProtocols;
using ClearMeasure.Bootcamp.WorkOrderAgentSpike;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AgentProtocols;

[TestFixture]
[NonParallelizable]
public class WorkOrderSpikeAgentHandlerTests
{
    [Test]
    public void InitializeAsync_ShouldAdvertiseHttpMcpCapability()
    {
        var handler = new WorkOrderSpikeAgentHandler(new StubMcpWorkOrderClient());

        var result = handler.InitializeAsync(new AcpInitializeRequest(1, "test-client", "1.0"), CancellationToken.None).Result;

        result.ProtocolVersion.ShouldBe(1);
        result.Capabilities.McpCapabilities.Http.ShouldBeTrue();
    }

    [Test]
    public async Task NewSessionAsync_WhenHttpMcpServerProvided_ShouldPreferSessionEndpointOverEnv()
    {
        Environment.SetEnvironmentVariable("CHURCHBULLETIN_MCP_URL", "https://env.example.com/mcp");
        var handler = new WorkOrderSpikeAgentHandler(new StubMcpWorkOrderClient());

        var session = await handler.NewSessionAsync(new AcpNewSessionRequest(
            "/tmp",
            [new AcpMcpServerInfo("http", "session-mcp", "https://session.example.com/mcp", [])]),
            CancellationToken.None);
        var context = new StubAcpAgentContext();
        await handler.PromptAsync(new AcpPromptRequest(session.SessionId, "hello"), context, CancellationToken.None);

        var message = string.Join("", context.Messages);
        message.ShouldContain("https://session.example.com/mcp");
        message.ShouldContain("Source=session/new");
        message.ShouldNotContain("https://env.example.com/mcp");
    }

    [Test]
    public async Task NewSessionAsync_WhenMcpServersEmpty_ShouldUseEnvFallback()
    {
        var previousUrl = Environment.GetEnvironmentVariable("CHURCHBULLETIN_MCP_URL");
        var previousKey = Environment.GetEnvironmentVariable("CHURCHBULLETIN_API_KEY");
        try
        {
            Environment.SetEnvironmentVariable("CHURCHBULLETIN_MCP_URL", "https://env.example.com/mcp");
            Environment.SetEnvironmentVariable("CHURCHBULLETIN_API_KEY", "env-key");
            var handler = new WorkOrderSpikeAgentHandler(new StubMcpWorkOrderClient());

            var session = await handler.NewSessionAsync(new AcpNewSessionRequest("/tmp", []), CancellationToken.None);
            var context = new StubAcpAgentContext();
            await handler.PromptAsync(new AcpPromptRequest(session.SessionId, "hello"), context, CancellationToken.None);

            var message = string.Join("", context.Messages);
            message.ShouldContain("env-mcp");
            message.ShouldContain("https://env.example.com/mcp");
            message.ShouldContain("Source=env-fallback");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHURCHBULLETIN_MCP_URL", previousUrl);
            Environment.SetEnvironmentVariable("CHURCHBULLETIN_API_KEY", previousKey);
        }
    }

    [Test]
    public void NewSessionAsync_WhenNoEndpointConfigured_ShouldStillReturnSessionId()
    {
        var previousUrl = Environment.GetEnvironmentVariable("CHURCHBULLETIN_MCP_URL");
        try
        {
            Environment.SetEnvironmentVariable("CHURCHBULLETIN_MCP_URL", null);
            var handler = new WorkOrderSpikeAgentHandler(new StubMcpWorkOrderClient());

            var result = handler.NewSessionAsync(new AcpNewSessionRequest("/tmp", []), CancellationToken.None).Result;

            result.SessionId.ShouldNotBeNullOrEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHURCHBULLETIN_MCP_URL", previousUrl);
        }
    }

    [Test]
    public void PromptAsync_Greeting_ShouldIncludeMcpEndpointSummary()
    {
        var handler = new WorkOrderSpikeAgentHandler(new StubMcpWorkOrderClient());
        var session = handler.NewSessionAsync(new AcpNewSessionRequest(
            "/tmp",
            [new AcpMcpServerInfo("http", "session-mcp", "https://session.example.com/mcp", [new AcpMcpHttpHeader("Authorization", "Bearer token")])]),
            CancellationToken.None).Result;
        var context = new StubAcpAgentContext();

        var result = handler.PromptAsync(new AcpPromptRequest(session.SessionId, "hello there"), context, CancellationToken.None).Result;

        result.StopReason.ShouldBe(AcpStopReason.EndTurn);
        var message = string.Join("", context.Messages);
        message.ShouldContain("MCP endpoint: Name=session-mcp");
        message.ShouldContain("Url=https://session.example.com/mcp");
        message.ShouldContain("headers=[Authorization]");
        message.ShouldContain("Source=session/new");
    }

    [Test]
    public void PromptAsync_ListWorkOrders_ShouldInvokeListWorkOrdersTool()
    {
        var client = new StubMcpWorkOrderClient { Output = "WO-1: Draft" };
        var handler = new WorkOrderSpikeAgentHandler(client);
        var session = handler.NewSessionAsync(new AcpNewSessionRequest(
            "/tmp",
            [new AcpMcpServerInfo("http", "session-mcp", "https://session.example.com/mcp", [])]),
            CancellationToken.None).Result;
        var context = new StubAcpAgentContext();

        var result = handler.PromptAsync(new AcpPromptRequest(session.SessionId, "list work orders"), context, CancellationToken.None).Result;

        result.StopReason.ShouldBe(AcpStopReason.EndTurn);
        client.Calls.Count.ShouldBe(1);
        client.Calls[0].ToolName.ShouldBe("list-work-orders");
        client.Calls[0].Endpoint.Url.ShouldBe("https://session.example.com/mcp");
        client.Calls[0].Endpoint.Source.ShouldBe("session/new");
        context.ToolCalls.Count.ShouldBe(2);
        context.ToolCalls[0].Status.ShouldBe(AcpToolStatus.Pending);
        context.ToolCalls[1].Status.ShouldBe(AcpToolStatus.Completed);
        string.Join("", context.Messages).ShouldContain("WO-1: Draft");
    }

    [Test]
    public void PromptAsync_CreateWorkOrder_WhenApproved_ShouldInvokeCreateWorkOrder()
    {
        var client = new StubMcpWorkOrderClient { Output = "Created WO-9" };
        var handler = new WorkOrderSpikeAgentHandler(client);
        var session = handler.NewSessionAsync(new AcpNewSessionRequest(
            "/tmp",
            [new AcpMcpServerInfo("http", "session-mcp", "https://session.example.com/mcp", [])]),
            CancellationToken.None).Result;
        var context = new StubAcpAgentContext { PermissionOutcome = new AcpPermissionOutcome(false, "allow_once") };

        var result = handler.PromptAsync(new AcpPromptRequest(session.SessionId, "create work order Fix the door"), context, CancellationToken.None).Result;

        result.StopReason.ShouldBe(AcpStopReason.EndTurn);
        client.Calls.Count.ShouldBe(1);
        client.Calls[0].ToolName.ShouldBe("create-work-order");
        client.Calls[0].Arguments["title"].ShouldBe("Fix the door");
        client.Calls[0].Arguments["description"].ShouldBe("Created via ACP spike");
        context.ToolCalls.Count.ShouldBe(2);
        context.ToolCalls[1].Status.ShouldBe(AcpToolStatus.Completed);
        string.Join("", context.Messages).ShouldContain("Created WO-9");
    }

    [Test]
    public void PromptAsync_CreateWorkOrder_WhenRejected_ShouldNotCallMcp()
    {
        var client = new StubMcpWorkOrderClient();
        var handler = new WorkOrderSpikeAgentHandler(client);
        var session = handler.NewSessionAsync(new AcpNewSessionRequest(
            "/tmp",
            [new AcpMcpServerInfo("http", "session-mcp", "https://session.example.com/mcp", [])]),
            CancellationToken.None).Result;
        var context = new StubAcpAgentContext { PermissionOutcome = new AcpPermissionOutcome(false, "reject_once") };

        var result = handler.PromptAsync(new AcpPromptRequest(session.SessionId, "create a work order Fix the door"), context, CancellationToken.None).Result;

        result.StopReason.ShouldBe(AcpStopReason.EndTurn);
        client.Calls.ShouldBeEmpty();
        string.Join("", context.Messages).ShouldContain("Permission declined; no work order change was executed.");
    }

    [Test]
    public void PromptAsync_CreateWorkOrder_WhenCancelled_ShouldReturnCancelled()
    {
        var client = new StubMcpWorkOrderClient();
        var handler = new WorkOrderSpikeAgentHandler(client);
        var session = handler.NewSessionAsync(new AcpNewSessionRequest(
            "/tmp",
            [new AcpMcpServerInfo("http", "session-mcp", "https://session.example.com/mcp", [])]),
            CancellationToken.None).Result;
        var context = new StubAcpAgentContext { PermissionOutcome = new AcpPermissionOutcome(true, null) };

        var result = handler.PromptAsync(new AcpPromptRequest(session.SessionId, "create a work order Fix the door"), context, CancellationToken.None).Result;

        result.StopReason.ShouldBe(AcpStopReason.Cancelled);
        client.Calls.ShouldBeEmpty();
    }

    [Test]
    public void PromptAsync_CreateWorkOrder_WhenNoEndpoint_ShouldReturnRefusal()
    {
        var previousUrl = Environment.GetEnvironmentVariable("CHURCHBULLETIN_MCP_URL");
        try
        {
            Environment.SetEnvironmentVariable("CHURCHBULLETIN_MCP_URL", null);
            var client = new StubMcpWorkOrderClient();
            var handler = new WorkOrderSpikeAgentHandler(client);
            var session = handler.NewSessionAsync(new AcpNewSessionRequest("/tmp", []), CancellationToken.None).Result;
            var context = new StubAcpAgentContext { PermissionOutcome = new AcpPermissionOutcome(false, "allow_once") };

            var result = handler.PromptAsync(new AcpPromptRequest(session.SessionId, "create a work order Fix the door"), context, CancellationToken.None).Result;

            result.StopReason.ShouldBe(AcpStopReason.Refusal);
            client.Calls.ShouldBeEmpty();
            string.Join("", context.Messages).ShouldContain("No MCP endpoint configured.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("CHURCHBULLETIN_MCP_URL", previousUrl);
        }
    }

    [Test]
    public void PromptAsync_ExecuteCommand_CompleteAssignCancel_ShouldMapCommandNames()
    {
        var cases = new (string Prompt, string ExpectedCommand)[]
        {
            ("complete WO-1", "InProgressToCompleteCommand"),
            ("assign WO-2", "DraftToAssignedCommand"),
            ("cancel WO-3", "AssignedToCancelledCommand"),
        };

        foreach (var (prompt, expected) in cases)
        {
            var client = new StubMcpWorkOrderClient { Output = "ok" };
            var handler = new WorkOrderSpikeAgentHandler(client);
            var session = handler.NewSessionAsync(new AcpNewSessionRequest(
                "/tmp",
                [new AcpMcpServerInfo("http", "session-mcp", "https://session.example.com/mcp", [])]),
                CancellationToken.None).Result;
            var context = new StubAcpAgentContext { PermissionOutcome = new AcpPermissionOutcome(false, "allow_once") };

            var result = handler.PromptAsync(new AcpPromptRequest(session.SessionId, prompt), context, CancellationToken.None).Result;

            result.StopReason.ShouldBe(AcpStopReason.EndTurn);
            client.Calls.Count.ShouldBe(1);
            client.Calls[0].ToolName.ShouldBe("execute-work-order-command");
            client.Calls[0].Arguments["commandName"].ShouldBe(expected);
        }
    }

    [Test]
    public async Task CancelAsync_ShouldComplete()
    {
        var handler = new WorkOrderSpikeAgentHandler(new StubMcpWorkOrderClient());

        await handler.CancelAsync("some-session", CancellationToken.None);
    }

    private sealed class StubMcpWorkOrderClient : IMcpWorkOrderClient
    {
        public string Output { get; init; } = "stub-tool-output";

        public List<(McpEndpointConfig Endpoint, string ToolName, IReadOnlyDictionary<string, object?> Arguments)> Calls { get; } = [];

        public Task<string> CallToolAsync(McpEndpointConfig endpoint, string toolName, IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
        {
            Calls.Add((endpoint, toolName, arguments));
            return Task.FromResult(Output);
        }
    }

    private sealed class StubAcpAgentContext : IAcpAgentContext
    {
        public List<string> Messages { get; } = [];

        public List<(string ToolCallId, string Title, AcpToolKind Kind, AcpToolStatus Status, string? RawOutput)> ToolCalls { get; } = [];

        public AcpPermissionOutcome PermissionOutcome { get; init; } = new(false, "allow_once");

        public Task SendMessageChunkAsync(string sessionId, string text, CancellationToken cancellationToken)
        {
            Messages.Add(text);
            return Task.CompletedTask;
        }

        public Task SendToolCallAsync(string sessionId, string toolCallId, string title, AcpToolKind kind, AcpToolStatus status, string? rawOutput = null, CancellationToken cancellationToken = default)
        {
            ToolCalls.Add((toolCallId, title, kind, status, rawOutput));
            return Task.CompletedTask;
        }

        public Task<AcpPermissionOutcome> RequestPermissionAsync(AcpPermissionRequest request, CancellationToken cancellationToken)
            => Task.FromResult(PermissionOutcome);
    }
}
