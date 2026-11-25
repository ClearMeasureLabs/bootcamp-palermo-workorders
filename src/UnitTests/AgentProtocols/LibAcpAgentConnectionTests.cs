using System.IO.Pipelines;
using ClearMeasure.Bootcamp.AgentProtocols;
using ClearMeasure.Bootcamp.AgentProtocols.LibAcp;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.AgentProtocols;

[TestFixture]
public class LibAcpAgentConnectionTests
{
    [Test]
    public void NoLibAcpTypesAppearInPublicAgentProtocolsSeam()
    {
        var publicSeamTypes = typeof(IAcpAgentHandler).Assembly.GetExportedTypes()
            .Where(t => t.Namespace != "ClearMeasure.Bootcamp.AgentProtocols.LibAcp")
            .ToList();

        var offendingNamespaces = publicSeamTypes
            .SelectMany(t => t.GetProperties()
                .Select(p => p.PropertyType.Namespace)
                .Concat(t.GetMethods()
                    .SelectMany(m => new[] { m.ReturnType }
                        .Concat(m.GetParameters().Select(p => p.ParameterType)))
                    .Select(pt => pt.Namespace)))
            .Where(n => n is not null && n.StartsWith("Acp"))
            .Distinct()
            .ToList();

        offendingNamespaces.ShouldBeEmpty();
    }

    [Test]
    public async Task LibAcpAgentConnection_OverPipedStreams_ShouldHandleInitializeNewSessionPromptPermissionAndCancel()
    {
        var stubHandler = new StubAgentHandler();
        var stubClient = new StubAcpClient();
        var clientToAgent = new Pipe();
        var agentToClient = new Pipe();
        IAcpAgentConnection? agentConnection = null;
        Acp.ClientSideConnection? clientConnection = null;

        try
        {
            agentConnection = LibAcpAgentConnection.CreateStream(
                clientToAgent.Reader.AsStream(),
                agentToClient.Writer.AsStream(),
                () => stubHandler);
            clientConnection = new Acp.ClientSideConnection(
                _ => stubClient,
                new Acp.Streaming.NdJsonStream(agentToClient.Reader.AsStream(), clientToAgent.Writer.AsStream()));

            var initializeResponse = await clientConnection.InitializeAsync(
                new Acp.Schema.InitializeRequest
                {
                    ProtocolVersion = AcpProtocol.Version,
                    ClientInfo = new Acp.Schema.Implementation { Name = "unit-test-client", Version = "1.0.0" },
                },
                CancellationToken.None);

            initializeResponse.ProtocolVersion.ShouldBe(1);
            initializeResponse.AgentCapabilities?.McpCapabilities?.Http.ShouldBe(true);
            stubHandler.InitializeRequest.ShouldNotBeNull();
            stubHandler.InitializeRequest!.ProtocolVersion.ShouldBe(1);
            stubHandler.InitializeRequest.ClientName.ShouldBe("unit-test-client");

            var newSessionResponse = await clientConnection.NewSessionAsync(
                new Acp.Schema.NewSessionRequest
                {
                    Cwd = "/tmp",
                    McpServers =
                    [
                        new Acp.Schema.McpServerHttp
                        {
                            Name = "churchbulletin",
                            Url = "http://localhost:5174/mcp",
                            Headers =
                            [
                                new Acp.Schema.HttpHeader { Name = "X-API-Key", Value = "test-key" },
                            ],
                        },
                    ],
                },
                CancellationToken.None);

            newSessionResponse.SessionId.Value.ShouldBe("session-1");
            var newSessionRequest = stubHandler.NewSessionRequest.ShouldNotBeNull();
            newSessionRequest.Cwd.ShouldBe("/tmp");
            var mcpServer = newSessionRequest.McpServers.ShouldHaveSingleItem();
            mcpServer.TransportType.ShouldBe("http");
            mcpServer.Name.ShouldBe("churchbulletin");
            mcpServer.Url.ShouldBe("http://localhost:5174/mcp");
            mcpServer.Headers.ShouldContain(new AcpMcpHttpHeader("X-API-Key", "test-key"));

            stubClient.PermissionResponder = _ => new Acp.Schema.RequestPermissionResponse
            {
                Outcome = new Acp.Schema.SelectedPermissionOutcome
                {
                    OptionId = new Acp.Schema.PermissionOptionId("allow_once"),
                },
            };

            var promptResponse = await clientConnection.PromptAsync(
                new Acp.Schema.PromptRequest
                {
                    SessionId = new Acp.Schema.SessionId("session-1"),
                    Prompt = [new Acp.Schema.TextContent { Text = "Run the work order pipeline" }],
                },
                CancellationToken.None);

            promptResponse.StopReason.ShouldBe(Acp.Schema.StopReason.EndTurn);
            var promptRequest = stubHandler.PromptRequest.ShouldNotBeNull();
            promptRequest.SessionId.ShouldBe("session-1");
            promptRequest.PromptText.ShouldBe("Run the work order pipeline");
            stubHandler.PermissionOutcome.ShouldBe(new AcpPermissionOutcome(false, "allow_once"));

            var updates = stubClient.ReceivedUpdates.Select(u => u.Update).ToList();
            var messageChunk = updates[0].ShouldBeOfType<Acp.Schema.AgentMessageChunk>();
            stubClient.ReceivedUpdates[0].SessionId.Value.ShouldBe("session-1");
            messageChunk.Content.ShouldBeOfType<Acp.Schema.TextContent>().Text.ShouldBe("chunk-1");
            var toolStart = updates[1].ShouldBeOfType<Acp.Schema.ToolCallStartUpdate>();
            toolStart.ToolCallId.Value.ShouldBe("tool-1");
            toolStart.Title.ShouldBe("Run pipeline stage");
            toolStart.Kind.ShouldBe(Acp.Schema.ToolKind.Execute);
            toolStart.Status.ShouldBe(Acp.Schema.ToolCallStatus.Pending);
            var toolUpdate = updates[2].ShouldBeOfType<Acp.Schema.ToolCallUpdateUpdate>();
            toolUpdate.ToolCallId.Value.ShouldBe("tool-1");
            toolUpdate.Status.ShouldBe(Acp.Schema.ToolCallStatus.Completed);
            toolUpdate.Content.ShouldHaveSingleItem().ShouldBeOfType<Acp.Schema.ToolCallContentBlock>().Content.ShouldBeOfType<Acp.Schema.TextContent>().Text.ShouldBe("pipeline ok");

            var permissionRequest = stubClient.LastPermissionRequest.ShouldNotBeNull();
            permissionRequest.SessionId.Value.ShouldBe("session-1");
            permissionRequest.ToolCall.ToolCallId.Value.ShouldBe("tool-1");
            permissionRequest.ToolCall.Title.ShouldBe("Run pipeline stage");
            permissionRequest.Options.Select(o => o.OptionId.Value).ShouldContain("allow_once");

            stubClient.PermissionResponder = _ => new Acp.Schema.RequestPermissionResponse
            {
                Outcome = new Acp.Schema.CancelledPermissionOutcome(),
            };

            var secondPromptResponse = await clientConnection.PromptAsync(
                new Acp.Schema.PromptRequest
                {
                    SessionId = new Acp.Schema.SessionId("session-1"),
                    Prompt = [new Acp.Schema.TextContent { Text = "Run it again" }],
                },
                CancellationToken.None);

            secondPromptResponse.StopReason.ShouldBe(Acp.Schema.StopReason.EndTurn);
            stubHandler.PermissionOutcome.ShouldBe(new AcpPermissionOutcome(true, null));

            await clientConnection.CancelAsync(
                new Acp.Schema.CancelNotification
                {
                    SessionId = new Acp.Schema.SessionId("session-1"),
                },
                CancellationToken.None);

            await stubHandler.CancelReceived.WaitAsync(TimeSpan.FromSeconds(5));
            stubHandler.CancelCalled.ShouldBeTrue();
        }
        finally
        {
            if (agentConnection is not null)
            {
                await agentConnection.DisposeAsync();
            }

            if (clientConnection is not null)
            {
                await clientConnection.DisposeAsync();
            }

            await clientToAgent.Writer.CompleteAsync();
            await agentToClient.Writer.CompleteAsync();
        }
    }

    [Test]
    public void CreateStdio_WhenHandlerFactoryNull_ShouldThrowArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => LibAcpAgentConnection.CreateStdio(null!));
    }

    [Test]
    public async Task NewSessionAsync_WhenSseAndStdioMcpServersProvided_ShouldMapAllServerTypes()
    {
        var stubHandler = new StubAgentHandler();
        var clientToAgent = new Pipe();
        var agentToClient = new Pipe();
        IAcpAgentConnection? agentConnection = null;
        Acp.ClientSideConnection? clientConnection = null;

        try
        {
            agentConnection = LibAcpAgentConnection.CreateStream(
                clientToAgent.Reader.AsStream(),
                agentToClient.Writer.AsStream(),
                () => stubHandler);
            clientConnection = new Acp.ClientSideConnection(
                _ => new StubAcpClient(),
                new Acp.Streaming.NdJsonStream(agentToClient.Reader.AsStream(), clientToAgent.Writer.AsStream()));

            await clientConnection.NewSessionAsync(
                new Acp.Schema.NewSessionRequest
                {
                    Cwd = "/tmp",
                    McpServers =
                    [
                        new Acp.Schema.McpServerHttp { Name = "h", Url = "http://localhost/mcp", Headers = [] },
                        new Acp.Schema.McpServerSse
                        {
                            Name = "s",
                            Url = "http://localhost/sse",
                            Headers = [new Acp.Schema.HttpHeader { Name = "Auth", Value = "v" }],
                        },
                        new Acp.Schema.McpServerStdio { Name = "st", Command = "echo", Args = ["hi"], Env = [] },
                    ],
                },
                CancellationToken.None);

            var servers = stubHandler.NewSessionRequest.ShouldNotBeNull().McpServers;
            servers.Count.ShouldBe(3);

            var http = servers[0];
            http.TransportType.ShouldBe("http");
            http.Name.ShouldBe("h");
            http.Url.ShouldBe("http://localhost/mcp");
            http.Headers.ShouldBeEmpty();

            var sse = servers[1];
            sse.TransportType.ShouldBe("sse");
            sse.Name.ShouldBe("s");
            sse.Url.ShouldBe("http://localhost/sse");
            sse.Headers.ShouldContain(new AcpMcpHttpHeader("Auth", "v"));

            var stdio = servers[2];
            stdio.TransportType.ShouldBe("stdio");
            stdio.Name.ShouldBe("st");
            stdio.Url.ShouldBeNull();
            stdio.Command.ShouldBe("echo");
            stdio.Args.ShouldNotBeNull().ShouldContain("hi");
        }
        finally
        {
            if (agentConnection is not null)
            {
                await agentConnection.DisposeAsync();
            }

            if (clientConnection is not null)
            {
                await clientConnection.DisposeAsync();
            }

            await clientToAgent.Writer.CompleteAsync();
            await agentToClient.Writer.CompleteAsync();
        }
    }

    [TestCase(AcpToolKind.Read, Acp.Schema.ToolKind.Read, AcpStopReason.EndTurn, Acp.Schema.StopReason.EndTurn)]
    [TestCase(AcpToolKind.Edit, Acp.Schema.ToolKind.Edit, AcpStopReason.Cancelled, Acp.Schema.StopReason.Cancelled)]
    [TestCase(AcpToolKind.Search, Acp.Schema.ToolKind.Search, AcpStopReason.Refusal, Acp.Schema.StopReason.Refusal)]
    [TestCase(AcpToolKind.Other, Acp.Schema.ToolKind.Other, AcpStopReason.MaxTokens, Acp.Schema.StopReason.MaxTokens)]
    public async Task PromptAsync_AllToolKindsAndStopReasons_ShouldMapCleanly(
        AcpToolKind kind,
        Acp.Schema.ToolKind expectedToolKind,
        AcpStopReason stopReason,
        Acp.Schema.StopReason expectedStopReason)
    {
        var stubHandler = new PromptingStubHandler(kind, stopReason);
        var stubClient = new StubAcpClient();
        var clientToAgent = new Pipe();
        var agentToClient = new Pipe();
        IAcpAgentConnection? agentConnection = null;
        Acp.ClientSideConnection? clientConnection = null;

        try
        {
            agentConnection = LibAcpAgentConnection.CreateStream(
                clientToAgent.Reader.AsStream(),
                agentToClient.Writer.AsStream(),
                () => stubHandler);
            clientConnection = new Acp.ClientSideConnection(
                _ => stubClient,
                new Acp.Streaming.NdJsonStream(agentToClient.Reader.AsStream(), clientToAgent.Writer.AsStream()));

            var promptResponse = await clientConnection.PromptAsync(
                new Acp.Schema.PromptRequest
                {
                    SessionId = new Acp.Schema.SessionId("session-1"),
                    Prompt = [new Acp.Schema.TextContent { Text = "map it" }],
                },
                CancellationToken.None);

            promptResponse.StopReason.ShouldBe(expectedStopReason);

            var updates = stubClient.ReceivedUpdates.Select(u => u.Update).ToList();
            var inProgress = updates.OfType<Acp.Schema.ToolCallUpdateUpdate>().Single(u => u.Status == Acp.Schema.ToolCallStatus.InProgress);
            inProgress.Kind.ShouldBe(expectedToolKind);
            var failed = updates.OfType<Acp.Schema.ToolCallUpdateUpdate>().Single(u => u.Status == Acp.Schema.ToolCallStatus.Failed);
            failed.Kind.ShouldBe(expectedToolKind);
        }
        finally
        {
            if (agentConnection is not null)
            {
                await agentConnection.DisposeAsync();
            }

            if (clientConnection is not null)
            {
                await clientConnection.DisposeAsync();
            }

            await clientToAgent.Writer.CompleteAsync();
            await agentToClient.Writer.CompleteAsync();
        }
    }

    private sealed class StubAgentHandler : IAcpAgentHandler
    {
        public AcpInitializeRequest? InitializeRequest { get; private set; }

        public AcpNewSessionRequest? NewSessionRequest { get; private set; }

        public AcpPromptRequest? PromptRequest { get; private set; }

        public AcpPermissionOutcome? PermissionOutcome { get; private set; }

        public bool CancelCalled { get; private set; }

        private readonly TaskCompletionSource _cancelSignal = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task CancelReceived => _cancelSignal.Task;

        public Task<AcpInitializeResult> InitializeAsync(AcpInitializeRequest request, CancellationToken cancellationToken)
        {
            InitializeRequest = request;
            return Task.FromResult(new AcpInitializeResult(
                AcpProtocol.Version,
                new AcpAgentInfo("stub-agent", "1.0.0", "Stub Agent"),
                new AcpAgentCapabilities(true, new AcpMcpCapabilities(true, false))));
        }

        public Task<AcpNewSessionResult> NewSessionAsync(AcpNewSessionRequest request, CancellationToken cancellationToken)
        {
            NewSessionRequest = request;
            return Task.FromResult(new AcpNewSessionResult("session-1"));
        }

        public async Task<AcpPromptResult> PromptAsync(AcpPromptRequest request, IAcpAgentContext context, CancellationToken cancellationToken)
        {
            PromptRequest = request;
            await context.SendMessageChunkAsync(request.SessionId, "chunk-1", cancellationToken);
            await context.SendToolCallAsync(request.SessionId, "tool-1", "Run pipeline stage", AcpToolKind.Execute, AcpToolStatus.Pending, cancellationToken: cancellationToken);
            await context.SendToolCallAsync(request.SessionId, "tool-1", "Run pipeline stage", AcpToolKind.Execute, AcpToolStatus.Completed, rawOutput: "pipeline ok", cancellationToken: cancellationToken);
            PermissionOutcome = await context.RequestPermissionAsync(
                new AcpPermissionRequest(
                    request.SessionId,
                    "tool-1",
                    "Run pipeline stage",
                    AcpToolKind.Execute,
                    [new AcpPermissionOption("allow_once", "Allow once", true)]),
                cancellationToken);
            return new AcpPromptResult(AcpStopReason.EndTurn);
        }

        public Task CancelAsync(string sessionId, CancellationToken cancellationToken)
        {
            CancelCalled = true;
            _cancelSignal.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class PromptingStubHandler(AcpToolKind toolKind, AcpStopReason stopReason) : IAcpAgentHandler
    {
        public Task<AcpInitializeResult> InitializeAsync(AcpInitializeRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<AcpNewSessionResult> NewSessionAsync(AcpNewSessionRequest request, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public async Task<AcpPromptResult> PromptAsync(AcpPromptRequest request, IAcpAgentContext context, CancellationToken cancellationToken)
        {
            await context.SendToolCallAsync(request.SessionId, "tool-1", "Mapped tool", toolKind, AcpToolStatus.InProgress, cancellationToken: cancellationToken);
            await context.SendToolCallAsync(request.SessionId, "tool-1", "Mapped tool", toolKind, AcpToolStatus.Failed, cancellationToken: cancellationToken);
            return new AcpPromptResult(stopReason);
        }

        public Task CancelAsync(string sessionId, CancellationToken cancellationToken)
            => throw new NotSupportedException();
    }

    private sealed class StubAcpClient : Acp.IClient
    {
        public List<Acp.Schema.SessionNotification> ReceivedUpdates { get; } = [];

        public Acp.Schema.RequestPermissionRequest? LastPermissionRequest { get; private set; }

        public Func<Acp.Schema.RequestPermissionRequest, Acp.Schema.RequestPermissionResponse> PermissionResponder { get; set; } =
            _ => new Acp.Schema.RequestPermissionResponse
            {
                Outcome = new Acp.Schema.CancelledPermissionOutcome(),
            };

        public Task SessionUpdateAsync(Acp.Schema.SessionNotification notification, CancellationToken cancellationToken)
        {
            ReceivedUpdates.Add(notification);
            return Task.CompletedTask;
        }

        public Task<Acp.Schema.RequestPermissionResponse> RequestPermissionAsync(Acp.Schema.RequestPermissionRequest request, CancellationToken cancellationToken)
        {
            LastPermissionRequest = request;
            return Task.FromResult(PermissionResponder(request));
        }
    }
}
