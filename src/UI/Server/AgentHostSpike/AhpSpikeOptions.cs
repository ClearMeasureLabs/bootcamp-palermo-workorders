// ReSharper disable PropertyCanBeMadeInitOnly.Global -- setters required for IOptions<T> configuration binding
namespace ClearMeasure.Bootcamp.UI.Server.AgentHostSpike;

/// <summary>
/// Configuration for the Agent Host Protocol spike, bound from the "AhpSpike" section.
/// </summary>
public sealed class AhpSpikeOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "AhpSpike";

    /// <summary>
    /// Number of chat delta actions to stream per turn.
    /// </summary>
    public int DeltaCount { get; set; } = 1000;

    /// <summary>
    /// Delay between consecutive chat delta actions.
    /// </summary>
    public int DeltaIntervalMilliseconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of action envelopes retained for reconnect replay.
    /// </summary>
    public int ReplayCapacity { get; set; } = 5000;
}
