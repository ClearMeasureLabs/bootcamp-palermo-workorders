using ClearMeasure.Bootcamp.UI.Server.AgentHostSpike;
using Microsoft.AgentHostProtocol;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.AgentHostSpike;

[TestFixture]
public class AhpSpikeJournalTests
{
    [Test]
    public void Should_AssignContiguousServerSeq_When_AppendingThreeActions()
    {
        var journal = CreateJournal();

        var first = journal.Append(Delta("a"));
        var second = journal.Append(Delta("b"));
        var third = journal.Append(Delta("c"));

        first.ServerSeq.ShouldBe(1);
        second.ServerSeq.ShouldBe(2);
        third.ServerSeq.ShouldBe(3);
        journal.ServerSeq.ShouldBe(3);
    }

    [Test]
    public void Should_ApplyReducerToChatState_When_AppendingTurnLifecycleActions()
    {
        var journal = CreateJournal();

        journal.Append(new StateAction(Started("t1")));
        journal.Append(new StateAction(new ChatResponsePartAction
        {
            TurnId = "t1",
            Part = new ResponsePart(new MarkdownResponsePart { Id = "part-1", Content = "" })
        }));
        journal.Append(Delta("a"));
        journal.Append(Delta("b"));

        MarkdownOf(journal.CaptureSnapshot().State.Chat).ShouldBe("ab");
    }

    [Test]
    public void Should_DeliverOnlyLaterEnvelopesInOrder_When_AttachingListener()
    {
        var journal = CreateJournal();
        var received = new List<ActionEnvelope>();
        long? attachedFromSeq = null;

        journal.Append(Delta("early"));
        journal.Attach(received.Add, snapshot => attachedFromSeq = snapshot.FromSeq);
        journal.Append(Delta("first"));
        journal.Append(Delta("second"));

        attachedFromSeq.ShouldBe(1);
        received.Select(envelope => envelope.ServerSeq).ShouldBe(new long[] { 2, 3 });
        received.ShouldAllBe(envelope => envelope.Channel == AhpSpikeJournal.ChatUri);
    }

    [Test]
    public void Should_NotifyOncePerAppend_When_SameListenerAttachedTwice()
    {
        var journal = CreateJournal();
        var received = new List<ActionEnvelope>();
        Action<ActionEnvelope> listener = received.Add;

        journal.Attach(listener, _ => { });
        journal.Attach(listener, _ => { });
        journal.Append(Delta("a"));
        journal.Append(Delta("b"));

        received.Select(envelope => envelope.ServerSeq).ShouldBe(new long[] { 1, 2 });
    }

    [Test]
    public void Should_StopNotifications_When_DetachingListener()
    {
        var journal = CreateJournal();
        var received = new List<ActionEnvelope>();
        Action<ActionEnvelope> listener = received.Add;

        journal.Attach(listener, _ => { });
        journal.Append(Delta("a"));
        journal.Detach(listener);
        journal.Append(Delta("b"));

        received.ShouldHaveSingleItem();
        received[0].ServerSeq.ShouldBe(1);
    }

    [Test]
    public void Should_ReplayMissedEnvelopes_When_ResumingWithinCapacity()
    {
        var journal = CreateJournal();
        for (var i = 1; i <= 5; i++)
        {
            journal.Append(Delta($"d{i}"));
        }

        var replayed = new List<ActionEnvelope>();
        var snapshotCount = 0;
        journal.Resume(2, replayed.Add, actions => replayed.AddRange(actions), _ => snapshotCount++);

        replayed.Select(envelope => envelope.ServerSeq).ShouldBe(new long[] { 3, 4, 5 });
        snapshotCount.ShouldBe(0);
    }

    [Test]
    public void Should_Snapshot_When_ResumingAfterEviction()
    {
        var journal = CreateJournal(replayCapacity: 3);
        for (var i = 1; i <= 10; i++)
        {
            journal.Append(Delta($"d{i}"));
        }

        long? snapshotFromSeq = null;
        var replayCount = 0;
        journal.Resume(1, _ => { }, actions => replayCount += actions.Count, snapshot => snapshotFromSeq = snapshot.FromSeq);

        snapshotFromSeq.ShouldBe(10);
        replayCount.ShouldBe(0);
    }

    [Test]
    public void Should_Snapshot_When_ResumingWithLastSeenBeyondServerSeq()
    {
        var journal = CreateJournal();
        for (var i = 1; i <= 3; i++)
        {
            journal.Append(Delta($"d{i}"));
        }

        var replayed = new List<ActionEnvelope>();
        Snapshot? snapshot = null;
        journal.Resume(9, replayed.Add, actions => replayed.AddRange(actions), captured => snapshot = captured);

        snapshot.ShouldNotBeNull();
        snapshot!.FromSeq.ShouldBe(3);
        replayed.ShouldBeEmpty();
    }

    [Test]
    public void Should_ReplayEmptyList_When_ResumingEmptyJournal()
    {
        var journal = CreateJournal();
        var replayed = new List<ActionEnvelope>();
        var snapshotCount = 0;

        journal.Resume(0, _ => { }, actions => replayed.AddRange(actions), _ => snapshotCount++);

        replayed.ShouldBeEmpty();
        snapshotCount.ShouldBe(0);
    }

    [Test]
    public void Should_ReturnDeepClone_When_CapturingSnapshot()
    {
        var journal = CreateJournal();
        journal.Append(new StateAction(Started("t1")));
        journal.Append(new StateAction(new ChatResponsePartAction
        {
            TurnId = "t1",
            Part = new ResponsePart(new MarkdownResponsePart { Id = "part-1", Content = "" })
        }));
        journal.Append(Delta("a"));
        var captured = journal.CaptureSnapshot();

        journal.Append(Delta("b"));

        captured.FromSeq.ShouldBe(3);
        MarkdownOf(captured.State.Chat).ShouldBe("a");
        MarkdownOf(journal.CaptureSnapshot().State.Chat).ShouldBe("ab");
    }

    private static AhpSpikeJournal CreateJournal(int replayCapacity = 5000) =>
        new(Options.Create(new AhpSpikeOptions { ReplayCapacity = replayCapacity }), TimeProvider.System);

    private static ChatTurnStartedAction Started(string turnId) =>
        new()
        {
            TurnId = turnId,
            StartedAt = "2026-01-01T00:00:00Z",
            Message = new Message { Text = "hello", Origin = new MessageOrigin { Kind = MessageKind.User } }
        };

    private static StateAction Delta(string content) =>
        new(new ChatDeltaAction { TurnId = "t1", PartId = "part-1", Content = content });

    private static string MarkdownOf(ChatState? state)
    {
        state.ShouldNotBeNull();
        List<ResponsePart>? parts = null;
        if (state.ActiveTurn is { } activeTurn)
        {
            parts = activeTurn.ResponseParts;
        }
        else if (state.Turns.Count > 0)
        {
            parts = state.Turns[^1].ResponseParts;
        }

        return parts!
            .Select(part => part.Value)
            .OfType<MarkdownResponsePart>()
            .First()
            .Content;
    }
}
