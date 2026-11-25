using System.Text.Json;
using ClearMeasure.Bootcamp.UI.Server.AgentHostSpike;
using Microsoft.AgentHostProtocol;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.AgentHostSpike;

[TestFixture]
public class AhpSpikeRpcHandlerTests
{
    [Test]
    public void Should_SendInitializeResultWithSnapshot_When_InitialSubscriptionsContainChatChannel()
    {
        var (_, _, handler, frames) = CreateHandler(user: "alice");

        handler.Handle(InitializeRequest(1, "client-1", [AhpSpikeJournal.ChatUri]));

        frames.Count.ShouldBe(1);
        using var document = JsonDocument.Parse(frames[0]);
        var root = document.RootElement;
        root.GetProperty("jsonrpc").GetString().ShouldBe("2.0");
        root.GetProperty("id").GetInt32().ShouldBe(1);
        var result = root.GetProperty("result");
        result.GetProperty("protocolVersion").GetString().ShouldBe("1.0.0");
        result.GetProperty("serverSeq").GetInt64().ShouldBe(0);
        result.GetProperty("serverInfo").GetProperty("name").GetString().ShouldBe("church-bulletin-ahp-spike-host");
        result.GetProperty("_meta").GetProperty("spike.user").GetString().ShouldBe("alice");
        var snapshots = result.GetProperty("snapshots");
        snapshots.GetArrayLength().ShouldBe(1);
        snapshots[0].GetProperty("resource").GetString().ShouldBe(AhpSpikeJournal.ChatUri);
        snapshots[0].GetProperty("fromSeq").GetInt64().ShouldBe(0);
        handler.ClientId.ShouldBe("client-1");
    }

    [Test]
    public void Should_SendEmptySnapshots_When_InitializeWithoutChatSubscription()
    {
        var (_, _, handler, frames) = CreateHandler();

        handler.Handle(InitializeRequest(1, "client-1", null));

        frames.Count.ShouldBe(1);
        using var document = JsonDocument.Parse(frames[0]);
        var result = document.RootElement.GetProperty("result");
        result.GetProperty("protocolVersion").GetString().ShouldBe("1.0.0");
        result.GetProperty("snapshots").GetArrayLength().ShouldBe(0);
        handler.ClientId.ShouldBe("client-1");
    }

    [Test]
    public void Should_ReturnUnsupportedVersionError_When_NoSharedProtocolVersion()
    {
        var (_, _, handler, frames) = CreateHandler();

        handler.Handle(InitializeRequest(1, "client-1", null, ["9.9.9"]));

        frames.Count.ShouldBe(1);
        using var document = JsonDocument.Parse(frames[0]);
        var error = document.RootElement.GetProperty("error");
        error.GetProperty("code").GetInt32().ShouldBe(-32005);
        error.GetProperty("message").GetString().ShouldNotBeNullOrEmpty();
        error.GetProperty("data").GetProperty("supportedVersions").GetArrayLength().ShouldBeGreaterThan(0);
        handler.ClientId.ShouldBeNull();
    }

    [Test]
    public void Should_ReturnInvalidParamsError_When_InitializeWithoutParams()
    {
        var (_, _, handler, frames) = CreateHandler();

        handler.Handle("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\"}");

        frames.Count.ShouldBe(1);
        using var document = JsonDocument.Parse(frames[0]);
        document.RootElement.GetProperty("error").GetProperty("code").GetInt32().ShouldBe(-32602);
    }

    [Test]
    public void Should_SendActionFramesInServerSeqOrder_When_InitializeSubscribesToChatChannel()
    {
        var (journal, _, handler, frames) = CreateHandler();

        handler.Handle(InitializeRequest(1, "client-1", [AhpSpikeJournal.ChatUri]));
        journal.Append(Delta("one"));
        journal.Append(Delta("two"));
        journal.Append(Delta("three"));

        frames.Count.ShouldBe(4);
        for (var i = 0; i < 3; i++)
        {
            using var document = JsonDocument.Parse(frames[i + 1]);
            var root = document.RootElement;
            root.GetProperty("jsonrpc").GetString().ShouldBe("2.0");
            root.GetProperty("method").GetString().ShouldBe("action");
            var parameters = root.GetProperty("params");
            parameters.GetProperty("channel").GetString().ShouldBe(AhpSpikeJournal.ChatUri);
            parameters.GetProperty("action").GetProperty("type").GetString().ShouldBe("chat/delta");
            parameters.GetProperty("serverSeq").GetInt64().ShouldBe(i + 1);
        }
    }

    [Test]
    public void Should_ReplayMissedActions_When_ReconnectingWithinCapacity()
    {
        var (journal, _, handler, frames) = CreateHandler();
        for (var i = 1; i <= 5; i++)
        {
            journal.Append(Delta($"d{i}"));
        }

        handler.Handle(ReconnectRequest(1, "client-1", 2, [AhpSpikeJournal.ChatUri, "ahp-other://missing"]));

        frames.Count.ShouldBe(1);
        using var document = JsonDocument.Parse(frames[0]);
        var result = document.RootElement.GetProperty("result");
        result.GetProperty("type").GetString().ShouldBe("replay");
        var actions = result.GetProperty("actions");
        actions.GetArrayLength().ShouldBe(3);
        actions[0].GetProperty("serverSeq").GetInt64().ShouldBe(3);
        actions[1].GetProperty("serverSeq").GetInt64().ShouldBe(4);
        actions[2].GetProperty("serverSeq").GetInt64().ShouldBe(5);
        var missing = result.GetProperty("missing");
        missing.GetArrayLength().ShouldBe(1);
        missing[0].GetString().ShouldBe("ahp-other://missing");
        handler.ClientId.ShouldBe("client-1");
    }

    [Test]
    public void Should_ReplayEmptyActions_When_ReconnectingWithoutChatChannel()
    {
        var (_, _, handler, frames) = CreateHandler();

        handler.Handle(ReconnectRequest(1, "client-2", 0, ["ahp-other://x"]));

        frames.Count.ShouldBe(1);
        using var document = JsonDocument.Parse(frames[0]);
        var result = document.RootElement.GetProperty("result");
        result.GetProperty("type").GetString().ShouldBe("replay");
        result.GetProperty("actions").GetArrayLength().ShouldBe(0);
        var missing = result.GetProperty("missing");
        missing.GetArrayLength().ShouldBe(1);
        missing[0].GetString().ShouldBe("ahp-other://x");
        handler.ClientId.ShouldBe("client-2");
    }

    [Test]
    public void Should_SendSnapshot_When_ReconnectingAfterEviction()
    {
        var journal = CreateJournal(replayCapacity: 3);
        for (var i = 1; i <= 10; i++)
        {
            journal.Append(Delta($"d{i}"));
        }

        var frames = new List<string>();
        var handler = new AhpSpikeRpcHandler(journal, CreateStreamer(journal, deltaCount: 2, intervalMs: 0), "alice", frames.Add);
        handler.Handle(ReconnectRequest(1, "client-1", 1, [AhpSpikeJournal.ChatUri]));

        frames.Count.ShouldBe(1);
        using var document = JsonDocument.Parse(frames[0]);
        var result = document.RootElement.GetProperty("result");
        result.GetProperty("type").GetString().ShouldBe("snapshot");
        var snapshots = result.GetProperty("snapshots");
        snapshots.GetArrayLength().ShouldBe(1);
        snapshots[0].GetProperty("resource").GetString().ShouldBe(AhpSpikeJournal.ChatUri);
        snapshots[0].GetProperty("fromSeq").GetInt64().ShouldBe(10);
        handler.ClientId.ShouldBe("client-1");
    }

    [Test]
    public void Should_SendSnapshotAndActionFrames_When_SubscribingToChatChannel()
    {
        var (journal, _, handler, frames) = CreateHandler();

        handler.Handle(SubscribeRequest(1, AhpSpikeJournal.ChatUri));
        journal.Append(Delta("a"));

        frames.Count.ShouldBe(2);
        using var resultDocument = JsonDocument.Parse(frames[0]);
        resultDocument.RootElement
            .GetProperty("result")
            .GetProperty("snapshot")
            .GetProperty("resource")
            .GetString().ShouldBe(AhpSpikeJournal.ChatUri);
        using var actionDocument = JsonDocument.Parse(frames[1]);
        var parameters = actionDocument.RootElement.GetProperty("params");
        parameters.GetProperty("channel").GetString().ShouldBe(AhpSpikeJournal.ChatUri);
        parameters.GetProperty("action").GetProperty("type").GetString().ShouldBe("chat/delta");
        parameters.GetProperty("action").GetProperty("content").GetString().ShouldBe("a");
        parameters.GetProperty("serverSeq").GetInt64().ShouldBe(1);
    }

    [Test]
    public void Should_ReturnUnknownChannelError_When_SubscribingToUnknownChannel()
    {
        var (_, _, handler, frames) = CreateHandler();

        handler.Handle(SubscribeRequest(1, "ahp-other://x"));

        frames.Count.ShouldBe(1);
        using var document = JsonDocument.Parse(frames[0]);
        document.RootElement.GetProperty("error").GetProperty("code").GetInt32().ShouldBe(-32602);
    }

    [Test]
    public void Should_SendEmptyResult_When_Ping()
    {
        var (_, _, handler, frames) = CreateHandler();

        handler.Handle("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"ping\"}");

        frames.Count.ShouldBe(1);
        using var document = JsonDocument.Parse(frames[0]);
        var result = document.RootElement.GetProperty("result");
        result.ValueKind.ShouldBe(JsonValueKind.Object);
        result.EnumerateObject().ShouldBeEmpty();
    }

    [Test]
    public void Should_StopActionFrames_When_Unsubscribed()
    {
        var (journal, _, handler, frames) = CreateHandler();

        handler.Handle(SubscribeRequest(1, AhpSpikeJournal.ChatUri));
        journal.Append(Delta("a"));
        var countBeforeUnsubscribe = frames.Count;
        handler.Handle("{\"jsonrpc\":\"2.0\",\"method\":\"unsubscribe\"}");
        journal.Append(Delta("b"));

        countBeforeUnsubscribe.ShouldBe(2);
        frames.Count.ShouldBe(2);
    }

    [Test]
    public async Task Should_StreamTurnWithOrigin_When_DispatchActionIsTurnStarted()
    {
        var (_, streamer, handler, frames) = CreateHandler();

        handler.Handle(InitializeRequest(1, "client-1", [AhpSpikeJournal.ChatUri]));
        handler.Handle(DispatchActionRequest(42, new StateAction(Started("t1"))));
        await streamer.Completion;

        frames.Count.ShouldBe(6);
        using var startedDocument = JsonDocument.Parse(frames[1]);
        var startedParameters = startedDocument.RootElement.GetProperty("params");
        startedParameters.GetProperty("action").GetProperty("type").GetString().ShouldBe("chat/turnStarted");
        startedParameters.GetProperty("origin").GetProperty("clientId").GetString().ShouldBe("client-1");
        startedParameters.GetProperty("origin").GetProperty("clientSeq").GetInt64().ShouldBe(42);
        using var deltaDocument = JsonDocument.Parse(frames[3]);
        var deltaAction = deltaDocument.RootElement.GetProperty("params").GetProperty("action");
        deltaAction.GetProperty("type").GetString().ShouldBe("chat/delta");
    }

    [Test]
    public void Should_NotStartStream_When_DispatchActionIsNotTurnStarted()
    {
        var (journal, _, handler, frames) = CreateHandler();
        handler.Handle(InitializeRequest(1, "client-1", null));

        handler.Handle(DispatchActionRequest(1, Delta("x")));

        journal.ServerSeq.ShouldBe(0);
        frames.Count.ShouldBe(1);
    }

    [Test]
    public void Should_IgnoreDispatchAction_When_NoParams()
    {
        var (journal, _, handler, frames) = CreateHandler();

        handler.Handle("{\"jsonrpc\":\"2.0\",\"method\":\"dispatchAction\"}");

        journal.ServerSeq.ShouldBe(0);
        frames.ShouldBeEmpty();
    }

    [Test]
    public void Should_ReturnMethodNotFoundError_When_UnknownMethodHasId()
    {
        var (_, _, handler, frames) = CreateHandler();

        handler.Handle("{\"jsonrpc\":\"2.0\",\"id\":5,\"method\":\"bogus\"}");

        frames.Count.ShouldBe(1);
        using var document = JsonDocument.Parse(frames[0]);
        var root = document.RootElement;
        root.GetProperty("id").GetInt32().ShouldBe(5);
        root.GetProperty("error").GetProperty("code").GetInt32().ShouldBe(-32601);
    }

    [TestCase("{\"jsonrpc\":\"2.0\",\"method\":\"bogus\"}")]
    [TestCase("not-json")]
    [TestCase("{\"jsonrpc\":\"2.0\",\"id\":9}")]
    public void Should_SendNothing_When_FrameHasNoDispatchableMethod(string frame)
    {
        var (_, _, handler, frames) = CreateHandler();

        handler.Handle(frame);

        frames.ShouldBeEmpty();
    }

    [Test]
    public void Should_StopActionFrames_When_Disposed()
    {
        var (journal, _, handler, frames) = CreateHandler();

        handler.Handle(SubscribeRequest(1, AhpSpikeJournal.ChatUri));
        journal.Append(Delta("a"));
        handler.Dispose();
        journal.Append(Delta("b"));

        frames.Count.ShouldBe(2);
    }

    private static (AhpSpikeJournal Journal, AhpSpikeTurnStreamer Streamer, AhpSpikeRpcHandler Handler, List<string> Frames) CreateHandler(string user = "alice")
    {
        var journal = CreateJournal();
        var streamer = CreateStreamer(journal, deltaCount: 2, intervalMs: 0);
        var frames = new List<string>();
        var handler = new AhpSpikeRpcHandler(journal, streamer, user, frames.Add);
        return (journal, streamer, handler, frames);
    }

    private static AhpSpikeJournal CreateJournal(int replayCapacity = 5000) =>
        new(Options.Create(new AhpSpikeOptions { ReplayCapacity = replayCapacity }), TimeProvider.System);

    private static AhpSpikeTurnStreamer CreateStreamer(AhpSpikeJournal journal, int deltaCount, int intervalMs) =>
        new(journal, Options.Create(new AhpSpikeOptions { DeltaCount = deltaCount, DeltaIntervalMilliseconds = intervalMs }), TimeProvider.System);

    private static ChatTurnStartedAction Started(string turnId) =>
        new()
        {
            TurnId = turnId,
            StartedAt = "2026-01-01T00:00:00Z",
            Message = new Message { Text = "hello", Origin = new MessageOrigin { Kind = MessageKind.User } }
        };

    private static StateAction Delta(string content) =>
        new(new ChatDeltaAction { TurnId = "t1", PartId = "part-1", Content = content });

    private static string InitializeRequest(int id, string clientId, List<string>? subscriptions, List<string>? protocolVersions = null)
    {
        var parameters = SystemTextJsonAhpSerializer.Default.Serialize(new InitializeParams
        {
            Channel = AhpSpikeJournal.ChatUri,
            ProtocolVersions = protocolVersions ?? ["1.0.0"],
            ClientId = clientId,
            InitialSubscriptions = subscriptions
        });
        return $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"method\":\"initialize\",\"params\":{parameters}}}";
    }

    private static string ReconnectRequest(int id, string clientId, long lastSeenServerSeq, List<string> subscriptions)
    {
        var parameters = SystemTextJsonAhpSerializer.Default.Serialize(new ReconnectParams
        {
            Channel = AhpSpikeJournal.ChatUri,
            ClientId = clientId,
            LastSeenServerSeq = lastSeenServerSeq,
            Subscriptions = subscriptions
        });
        return $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"method\":\"reconnect\",\"params\":{parameters}}}";
    }

    private static string SubscribeRequest(int id, string channel)
    {
        var parameters = SystemTextJsonAhpSerializer.Default.Serialize(new SubscribeParams { Channel = channel });
        return $"{{\"jsonrpc\":\"2.0\",\"id\":{id},\"method\":\"subscribe\",\"params\":{parameters}}}";
    }

    private static string DispatchActionRequest(long clientSeq, StateAction action)
    {
        var parameters = SystemTextJsonAhpSerializer.Default.Serialize(new DispatchActionParams
        {
            Channel = AhpSpikeJournal.ChatUri,
            ClientSeq = clientSeq,
            Action = action
        });
        return $"{{\"jsonrpc\":\"2.0\",\"method\":\"dispatchAction\",\"params\":{parameters}}}";
    }
}
