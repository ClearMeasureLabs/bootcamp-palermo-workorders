using Microsoft.AgentHostProtocol;
using Microsoft.Extensions.Options;

namespace ClearMeasure.Bootcamp.UI.Server.AgentHostSpike;

/// <summary>
/// Streams one simulated agent turn: a markdown response part, a burst of chat deltas, then turn complete.
/// </summary>
public sealed class AhpSpikeTurnStreamer
{
    private readonly AhpSpikeJournal _journal;
    private readonly int _deltaCount;
    private readonly int _deltaIntervalMilliseconds;
    private readonly TimeProvider _timeProvider;
    private int _streaming;

    /// <summary>
    /// Creates the streamer.
    /// </summary>
    public AhpSpikeTurnStreamer(AhpSpikeJournal journal, IOptions<AhpSpikeOptions> options, TimeProvider timeProvider)
    {
        _journal = journal;
        _deltaCount = options.Value.DeltaCount;
        _deltaIntervalMilliseconds = options.Value.DeltaIntervalMilliseconds;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Whether a turn is currently being streamed.
    /// </summary>
    public bool IsStreaming => Volatile.Read(ref _streaming) == 1;

    /// <summary>
    /// Completes when the current streamed turn finishes.
    /// </summary>
    public Task Completion { get; private set; } = Task.CompletedTask;

    /// <summary>
    /// Starts a new turn by echoing <paramref name="started"/> and launching the background stream.
    /// Returns false when another turn is already streaming.
    /// </summary>
    public bool TryStart(ChatTurnStartedAction started, ActionOrigin? origin)
    {
        if (Interlocked.CompareExchange(ref _streaming, 1, 0) != 0)
        {
            return false;
        }

        _journal.Append(new StateAction(started), origin);
        Completion = Task.Run(() => StreamAsync(started.TurnId));
        return true;
    }

    private async Task StreamAsync(string turnId)
    {
        var start = _timeProvider.GetTimestamp();
        _journal.Append(new StateAction(new ChatResponsePartAction
        {
            TurnId = turnId,
            Part = new ResponsePart(new MarkdownResponsePart { Id = "part-1", Content = "" })
        }));
        try
        {
            for (var i = 1; i <= _deltaCount; i++)
            {
                if (_deltaIntervalMilliseconds > 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(_deltaIntervalMilliseconds), _timeProvider).ConfigureAwait(false);
                }

                _journal.Append(new StateAction(new ChatDeltaAction
                {
                    TurnId = turnId,
                    PartId = "part-1",
                    Content = $"[{i:D4}] "
                }));
            }
        }
        finally
        {
            _journal.Append(new StateAction(new ChatTurnCompleteAction
            {
                TurnId = turnId,
                Duration = (long)_timeProvider.GetElapsedTime(start).TotalMilliseconds
            }));
            Interlocked.Exchange(ref _streaming, 0);
        }
    }
}
