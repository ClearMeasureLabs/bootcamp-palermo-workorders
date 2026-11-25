using Microsoft.AgentHostProtocol;

namespace ClearMeasure.Bootcamp.UI.Client.AgentHostSpike;

/// <summary>
/// Reduces AHP chat <see cref="ActionEnvelope"/>s and <see cref="Snapshot"/>s for the spike
/// channel into the UI state shown on the spike page. The spike host serves one channel, so
/// <c>serverSeq</c> values on it are contiguous and gap/duplicate accounting is exact.
/// Pure logic with no I/O.
/// </summary>
public sealed class AhpSpikeChatProjection
{
    /// <summary>Channel URI of the spike chat.</summary>
    public const string ChatUri = "ahp-chat:/spike-1000";

    /// <summary>Chat state reduced from the latest snapshot plus all applied actions.</summary>
    public ChatState? State { get; private set; }

    /// <summary>Highest serverSeq applied so far.</summary>
    public long LastSeenServerSeq { get; private set; }

    /// <summary>Number of <see cref="ChatDeltaAction"/> envelopes applied.</summary>
    public int DeltaActionsReceived { get; private set; }

    /// <summary>Number of actions applied from a reconnect replay.</summary>
    public int ReplayedActions { get; private set; }

    /// <summary>Number of missing serverSeq values observed between consecutive envelopes.</summary>
    public long Gaps { get; private set; }

    /// <summary>Number of envelopes dropped as duplicates (serverSeq not increasing).</summary>
    public int Duplicates { get; private set; }

    /// <summary>Number of snapshots applied.</summary>
    public int SnapshotsApplied { get; private set; }

    /// <summary>Markdown content of the active turn's first markdown part, else the last
    /// completed turn's first markdown part, else an empty string.</summary>
    public string Text
    {
        get
        {
            var parts = State?.ActiveTurn?.ResponseParts ?? LastCompletedTurn()?.ResponseParts;
            return FirstMarkdownContent(parts) ?? "";
        }
    }

    /// <summary>Number of deltas already rendered into <see cref="Text" /> (host deltas are "[0001] ").</summary>
    public int DeltasRendered => Text.Count(c => c == '[');

    /// <summary>Human-readable status of the current turn.</summary>
    public string TurnStatus => State is null ? "not connected" : DescribeTurn(State);

    /// <summary>Applies a snapshot and resets the reduced chat state.</summary>
    /// <param name="snapshot">Snapshot for the spike chat channel.</param>
    public void Seed(Snapshot snapshot)
    {
        State = snapshot.State.Chat ?? throw new InvalidOperationException("Snapshot has no chat state");
        LastSeenServerSeq = snapshot.FromSeq;
        SnapshotsApplied++;
    }

    /// <summary>Applies one action envelope to the reduced state.</summary>
    /// <param name="envelope">Envelope from the subscription feed.</param>
    /// <param name="fromReplay">Whether the envelope came from a reconnect replay.</param>
    /// <returns>Outcome of the application.</returns>
    public AhpSpikeApplyResult Apply(ActionEnvelope envelope, bool fromReplay = false)
    {
        var state = State ?? throw new InvalidOperationException("No snapshot has been seeded");
        var verdict = Classify(envelope);
        if (verdict != AhpSpikeApplyResult.Applied)
        {
            return verdict;
        }

        CountGap(envelope.ServerSeq);
        Reducers.ApplyToChat(state, envelope.Action);
        LastSeenServerSeq = envelope.ServerSeq;
        CountApplied(envelope, fromReplay);
        return AhpSpikeApplyResult.Applied;
    }

    private AhpSpikeApplyResult Classify(ActionEnvelope envelope)
    {
        if (envelope.Channel != ChatUri)
        {
            return AhpSpikeApplyResult.Ignored;
        }

        if (envelope.ServerSeq <= LastSeenServerSeq)
        {
            Duplicates++;
            return AhpSpikeApplyResult.Duplicate;
        }

        return AhpSpikeApplyResult.Applied;
    }

    private void CountGap(long serverSeq)
    {
        if (serverSeq > LastSeenServerSeq + 1)
        {
            Gaps += serverSeq - LastSeenServerSeq - 1;
        }
    }

    private void CountApplied(ActionEnvelope envelope, bool fromReplay)
    {
        if (envelope.Action.Value is ChatDeltaAction)
        {
            DeltaActionsReceived++;
        }

        if (fromReplay)
        {
            ReplayedActions++;
        }
    }

    private static string DescribeTurn(ChatState state)
    {
        if (state.ActiveTurn is not null)
        {
            return "streaming";
        }

        return state.Turns.Count > 0 ? "complete" : "idle";
    }

    private Turn? LastCompletedTurn() => State?.Turns.LastOrDefault();

    private static string? FirstMarkdownContent(List<ResponsePart>? parts)
        => parts?.Select(p => p.Value).OfType<MarkdownResponsePart>().FirstOrDefault()?.Content;
}

/// <summary>Outcome of applying one <see cref="ActionEnvelope"/> to the spike projection.</summary>
public enum AhpSpikeApplyResult
{
    /// <summary>Action was applied to the reduced state.</summary>
    Applied,

    /// <summary>Action had a non-increasing serverSeq and was dropped.</summary>
    Duplicate,

    /// <summary>Action was for a different channel and was ignored.</summary>
    Ignored
}
