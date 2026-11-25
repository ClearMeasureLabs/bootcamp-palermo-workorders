using Microsoft.AgentHostProtocol;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.AgentHostSpike;

/// <summary>
/// Thread-safe append-only journal of AHP action envelopes for the spike chat channel.
/// </summary>
public sealed class AhpSpikeJournal
{
    private readonly IOptions<AhpSpikeOptions> _options;
    private readonly Lock _gate = new();
    private readonly ChatState _state;
    private readonly LinkedList<ActionEnvelope> _retained = new();
    private readonly List<Action<ActionEnvelope>> _listeners = new();
    private long _serverSeq;
    private long _oldestRetainedSeq;

    /// <summary>
    /// URI of the single spike chat channel.
    /// </summary>
    public const string ChatUri = "ahp-chat:/spike-1000";

    /// <summary>
    /// Creates the journal in its initial idle chat state.
    /// </summary>
    public AhpSpikeJournal(IOptions<AhpSpikeOptions> options, TimeProvider timeProvider)
    {
        _options = options;
        _state = new ChatState
        {
            Resource = ChatUri,
            Title = "AHP spike: 1,000 streamed deltas",
            Status = SessionStatus.Idle,
            ModifiedAt = timeProvider.GetUtcNow().ToString("O"),
            Turns = []
        };
    }

    /// <summary>
    /// Current server sequence number.
    /// </summary>
    public long ServerSeq
    {
        get { lock (_gate) return _serverSeq; }
    }

    /// <summary>
    /// Appends an action to the journal, applies it to the chat state and notifies all listeners in order.
    /// </summary>
    public ActionEnvelope Append(StateAction action, ActionOrigin? origin = null)
    {
        lock (_gate)
        {
            _serverSeq++;
            var envelope = new ActionEnvelope
            {
                Channel = ChatUri,
                Action = action,
                ServerSeq = _serverSeq,
                Origin = origin
            };
            Reducers.ApplyToChat(_state, action);
            _retained.AddLast(envelope);
            if (_retained.Count == 1)
            {
                _oldestRetainedSeq = _serverSeq;
            }
            while (_retained.Count > _options.Value.ReplayCapacity)
            {
                _retained.RemoveFirst();
                _oldestRetainedSeq++;
            }
            foreach (var listener in _listeners.ToArray())
            {
                listener(envelope);
            }

            return envelope;
        }
    }

    /// <summary>
    /// Attaches a listener; <paramref name="onAttached"/> is invoked with the current snapshot in the same
    /// critical section as the listener registration, so every later notification has a higher server sequence.
    /// </summary>
    public void Attach(Action<ActionEnvelope> listener, Action<Snapshot> onAttached)
    {
        lock (_gate)
        {
            onAttached(CaptureSnapshotUnlocked());
            AddListenerUnlocked(listener);
        }
    }

    /// <summary>
    /// Replays retained envelopes newer than <paramref name="lastSeenServerSeq"/>, or falls back to a full
    /// snapshot when the requested history has been evicted.
    /// </summary>
    public void Resume(
        long lastSeenServerSeq,
        Action<ActionEnvelope> listener,
        Action<IReadOnlyList<ActionEnvelope>> onReplay,
        Action<Snapshot> onSnapshot)
    {
        lock (_gate)
        {
            if (CanReplay(lastSeenServerSeq))
            {
                onReplay(_retained.Where(envelope => envelope.ServerSeq > lastSeenServerSeq).ToList());
            }
            else
            {
                onSnapshot(CaptureSnapshotUnlocked());
            }

            AddListenerUnlocked(listener);
        }
    }

    /// <summary>
    /// Removes a previously attached listener.
    /// </summary>
    public void Detach(Action<ActionEnvelope> listener)
    {
        lock (_gate)
        {
            _listeners.Remove(listener);
        }
    }

    /// <summary>
    /// Captures a deep clone of the current chat state with the latest server sequence.
    /// </summary>
    public Snapshot CaptureSnapshot()
    {
        lock (_gate)
        {
            return CaptureSnapshotUnlocked();
        }
    }

    private void AddListenerUnlocked(Action<ActionEnvelope> listener)
    {
        if (!_listeners.Contains(listener))
        {
            _listeners.Add(listener);
        }
    }

    private bool CanReplay(long lastSeenServerSeq)
    {
        if (lastSeenServerSeq > _serverSeq)
        {
            return false;
        }

        if (_retained.Count == 0)
        {
            return true;
        }

        return lastSeenServerSeq >= _oldestRetainedSeq - 1;
    }

    private Snapshot CaptureSnapshotUnlocked() =>
        new()
        {
            Resource = ChatUri,
            State = new SnapshotState { Chat = CloneStateUnlocked() },
            FromSeq = _serverSeq
        };

    private ChatState CloneStateUnlocked()
    {
        var json = SystemTextJsonAhpSerializer.Default.Serialize(_state);
        return SystemTextJsonAhpSerializer.Default.Deserialize<ChatState>(json);
    }
}
