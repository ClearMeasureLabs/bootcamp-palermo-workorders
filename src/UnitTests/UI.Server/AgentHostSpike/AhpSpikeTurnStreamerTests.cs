using ClearMeasure.Bootcamp.UI.Server.AgentHostSpike;
using Microsoft.AgentHostProtocol;
using Microsoft.Extensions.Options;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Server.AgentHostSpike;

[TestFixture]
public class AhpSpikeTurnStreamerTests
{
    [Test]
    public async Task Should_StreamCompleteTurn_When_TryStartRequested()
    {
        var journal = CreateJournal();
        var streamer = CreateStreamer(journal, deltaCount: 5, intervalMs: 0);

        streamer.TryStart(Started("t1"), null).ShouldBeTrue();
        await streamer.Completion;

        journal.ServerSeq.ShouldBe(8);
        streamer.IsStreaming.ShouldBeFalse();
        var chat = journal.CaptureSnapshot().State.Chat;
        chat.ShouldNotBeNull();
        chat.Turns.Count.ShouldBe(1);
        MarkdownOf(chat).ShouldBe("[0001] [0002] [0003] [0004] [0005] ");
    }

    [Test]
    public async Task Should_ReturnFalse_When_TryStartCalledWhileStreaming()
    {
        var journal = CreateJournal();
        var streamer = CreateStreamer(journal, deltaCount: 3, intervalMs: 20);

        streamer.TryStart(Started("t1"), null).ShouldBeTrue();
        streamer.IsStreaming.ShouldBeTrue();
        var secondStart = streamer.TryStart(Started("t2"), null);
        await streamer.Completion;

        secondStart.ShouldBeFalse();
        streamer.IsStreaming.ShouldBeFalse();
        journal.ServerSeq.ShouldBe(6);
        MarkdownOf(journal.CaptureSnapshot().State.Chat).ShouldBe("[0001] [0002] [0003] ");
    }

    [Test]
    public async Task Should_CompleteTurn_When_DeltaIntervalIsPositive()
    {
        var journal = CreateJournal();
        var streamer = CreateStreamer(journal, deltaCount: 2, intervalMs: 1);

        streamer.TryStart(Started("t1"), null).ShouldBeTrue();
        await streamer.Completion;

        journal.ServerSeq.ShouldBe(5);
        MarkdownOf(journal.CaptureSnapshot().State.Chat).ShouldBe("[0001] [0002] ");
    }

    private static AhpSpikeJournal CreateJournal() =>
        new(Options.Create(new AhpSpikeOptions()), TimeProvider.System);

    private static AhpSpikeTurnStreamer CreateStreamer(AhpSpikeJournal journal, int deltaCount, int intervalMs) =>
        new(journal, Options.Create(new AhpSpikeOptions { DeltaCount = deltaCount, DeltaIntervalMilliseconds = intervalMs }), TimeProvider.System);

    private static ChatTurnStartedAction Started(string turnId) =>
        new()
        {
            TurnId = turnId,
            StartedAt = "2026-01-01T00:00:00Z",
            Message = new Message { Text = "hello", Origin = new MessageOrigin { Kind = MessageKind.User } }
        };

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
