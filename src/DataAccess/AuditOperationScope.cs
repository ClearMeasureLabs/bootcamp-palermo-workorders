using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.DataAccess;

internal static class AuditOperationScope
{
    internal static IDisposable Begin(string action, string? reportedActor)
    {
        var metadata = AuditContext.Current;
        var actor = metadata.ActorUserName;
        if ((!AuditContext.HasCurrent || string.Equals(actor, "system", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(actor, "anonymous", StringComparison.OrdinalIgnoreCase)) &&
            !string.IsNullOrWhiteSpace(reportedActor))
            actor = $"demo:{reportedActor}";
        return AuditContext.Begin(metadata with { ActorUserName = actor, Action = action });
    }
}
