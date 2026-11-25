using ClearMeasure.Bootcamp.UI.Client.AgentHostSpike;
using Microsoft.AgentHostProtocol;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client.AgentHostSpike;

[TestFixture]
public class AhpSpikeChatProjectionTests
{
    [Test]
    public void Should_ReturnNotConnectedStatus_When_NotSeeded()
    {
        var projection = new AhpSpikeChatProjection();

        projection.TurnStatus.ShouldBe("not connected");
    }

    [Test]
    public void Should_ReturnIdleStatus_When_SeededWithNoTurns()
    {
        var projection = SeededProjection();

        projection.TurnStatus.ShouldBe("idle");
    }

    [Test]
    public void Should_ReturnStreamingStatus_When_TurnStarted()
    {
        var projection = SeededProjection();

        projection.Apply(Envelope(1, TurnStarted("t1")));

        projection.TurnStatus.ShouldBe("streaming");
    }

    [Test]
    public void Should_ReturnCompleteStatus_When_TurnCompleted()
    {
        var projection = SeededProjection();
        projection.Apply(Envelope(1, TurnStarted("t1")));

        projection.Apply(Envelope(2, TurnComplete("t1")));

        projection.TurnStatus.ShouldBe("complete");
    }

    [Test]
    public void Should_Throw_When_SeedingSnapshotWithoutChat()
    {
        var projection = new AhpSpikeChatProjection();
        var snapshot = new Snapshot
        {
            Resource = AhpSpikeChatProjection.ChatUri,
            State = new SnapshotState(),
            FromSeq = 0
        };

        Should.Throw<InvalidOperationException>(() => projection.Seed(snapshot));
    }

    [Test]
    public void Should_Throw_When_ApplyingBeforeSeed()
    {
        var projection = new AhpSpikeChatProjection();

        Should.Throw<InvalidOperationException>(() => projection.Apply(Envelope(1, TurnStarted("t1"))));
    }

    [Test]
    public void Should_ReturnIgnoredAndKeepLastSeen_When_ApplyingOtherChannel()
    {
        var projection = SeededProjection();
        projection.Apply(Envelope(1, TurnStarted("t1")));

        var result = projection.Apply(Envelope(2, TurnStarted("t1"), "ahp-chat:/other"));

        result.ShouldBe(AhpSpikeApplyResult.Ignored);
        projection.LastSeenServerSeq.ShouldBe(1);
    }

    [Test]
    public void Should_ReduceFullTurn_When_ApplyingLifecycleAndDeltas()
    {
        var projection = SeededProjection();
        projection.Apply(Envelope(1, TurnStarted("t1")));
        projection.Apply(Envelope(2, ResponsePart("t1")));
        projection.Apply(Envelope(3, Delta("[0001] ")));
        projection.Apply(Envelope(4, Delta("[0002] ")));

        projection.Text.ShouldBe("[0001] [0002] ");
        projection.DeltasRendered.ShouldBe(2);
        projection.DeltaActionsReceived.ShouldBe(2);
        projection.Gaps.ShouldBe(0);
    }

    [Test]
    public void Should_KeepTurnContent_When_TurnCompleted()
    {
        var projection = SeededProjection();
        projection.Apply(Envelope(1, TurnStarted("t1")));
        projection.Apply(Envelope(2, ResponsePart("t1")));
        projection.Apply(Envelope(3, Delta("[0001] ")));

        projection.Apply(Envelope(4, TurnComplete("t1")));

        projection.Text.ShouldBe("[0001] ");
    }

    [Test]
    public void Should_ReturnDuplicateAndKeepText_When_ApplyingNonIncreasingSeq()
    {
        var projection = SeededProjection();
        projection.Apply(Envelope(1, TurnStarted("t1")));
        projection.Apply(Envelope(2, ResponsePart("t1")));
        projection.Apply(Envelope(3, Delta("[0001] ")));

        var result = projection.Apply(Envelope(3, Delta("[0002] ")));

        result.ShouldBe(AhpSpikeApplyResult.Duplicate);
        projection.Duplicates.ShouldBe(1);
        projection.Text.ShouldBe("[0001] ");
    }

    [Test]
    public void Should_CountMissingSeqsAsGaps_When_SeqJumps()
    {
        var projection = SeededProjection();
        projection.Apply(Envelope(1, TurnStarted("t1")));

        projection.Apply(Envelope(4, ResponsePart("t1")));

        projection.Gaps.ShouldBe(2);
        projection.LastSeenServerSeq.ShouldBe(4);
    }

    [Test]
    public void Should_CountReplayedActions_When_ApplyingFromReplay()
    {
        var projection = SeededProjection();

        projection.Apply(Envelope(1, TurnStarted("t1")), fromReplay: true);

        projection.ReplayedActions.ShouldBe(1);
    }

    [Test]
    public void Should_SetLastSeenAndIncrementSnapshots_When_Seeding()
    {
        var projection = new AhpSpikeChatProjection();

        projection.Seed(EmptyChatSnapshot(fromSeq: 7));

        projection.LastSeenServerSeq.ShouldBe(7);
        projection.SnapshotsApplied.ShouldBe(1);
    }

    [Test]
    public void Should_ReturnEmptyText_When_NoTurns()
    {
        var projection = SeededProjection();

        projection.Text.ShouldBe("");
    }

    private static AhpSpikeChatProjection SeededProjection()
    {
        var projection = new AhpSpikeChatProjection();
        projection.Seed(EmptyChatSnapshot(fromSeq: 0));

        return projection;
    }

    private static Snapshot EmptyChatSnapshot(long fromSeq) => new()
    {
        Resource = AhpSpikeChatProjection.ChatUri,
        State = new SnapshotState
        {
            Chat = new ChatState
            {
                Resource = AhpSpikeChatProjection.ChatUri,
                Title = "t",
                Status = SessionStatus.Idle,
                ModifiedAt = "2026-01-01T00:00:00Z",
                Turns = []
            }
        },
        FromSeq = fromSeq
    };

    private static ActionEnvelope Envelope(long seq, object variant, string? channel = null) =>
        new()
        {
            Channel = channel ?? AhpSpikeChatProjection.ChatUri,
            Action = new StateAction(variant),
            ServerSeq = seq
        };

    private static ChatTurnStartedAction TurnStarted(string turnId) => new()
    {
        TurnId = turnId,
        StartedAt = "2026-01-01T00:00:00Z",
        Message = new Message { Text = "go", Origin = new MessageOrigin { Kind = MessageKind.User } }
    };

    private static ChatResponsePartAction ResponsePart(string turnId) => new()
    {
        TurnId = turnId,
        Part = new ResponsePart(new MarkdownResponsePart { Id = "part-1", Content = "" })
    };

    private static ChatDeltaAction Delta(string content) =>
        new() { TurnId = "t1", PartId = "part-1", Content = content };

    private static ChatTurnCompleteAction TurnComplete(string turnId) =>
        new() { TurnId = turnId, Duration = 5 };
}
