namespace ClearMeasure.Bootcamp.Core;

/// <summary>Operation-local audit metadata propagated across asynchronous calls.</summary>
public sealed record AuditContext(
    string ActorUserName = "system",
    string? ActorDisplayName = null,
    string? IpAddress = null,
    string? UserAgent = null,
    string? CorrelationId = null,
    string? Action = null)
{
    private static readonly AsyncLocal<AuditContext?> Ambient = new();

    /// <summary>Whether an operation explicitly supplied audit metadata.</summary>
    public static bool HasCurrent => Ambient.Value is not null;

    /// <summary>Metadata for the current asynchronous operation.</summary>
    public static AuditContext Current => Ambient.Value ?? new AuditContext();

    /// <summary>Temporarily installs metadata and restores the previous context on disposal.</summary>
    public static IDisposable Begin(AuditContext context)
    {
        var previous = Ambient.Value;
        Ambient.Value = context;
        return new Scope(previous);
    }

    private sealed class Scope(AuditContext? previous) : IDisposable
    {
        public void Dispose() => Ambient.Value = previous;
    }
}
