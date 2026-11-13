using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.DataAccess;

internal static class AuditLogFactory
{
    internal static AuditLog Create(string action, string entityType, Guid? entityId,
        string? previousValue, string? newValue, Guid? workOrderId = null)
    {
        var metadata = AuditContext.Current;
        return new AuditLog
        {
            Action = Bound(action, 255)!,
            EntityType = Bound(entityType, 255)!,
            EntityId = entityId,
            WorkOrderId = workOrderId,
            PreviousValue = previousValue,
            NewValue = newValue,
            ActorUserName = Bound(metadata.ActorUserName, 255)!,
            ActorDisplayName = Bound(metadata.ActorDisplayName, 500),
            IpAddress = Bound(metadata.IpAddress, 45),
            UserAgent = Bound(metadata.UserAgent, 500),
            CorrelationId = Bound(metadata.CorrelationId, 64),
            Timestamp = DateTime.UtcNow
        };
    }

    private static string? Bound(string? value, int limit) =>
        value?.Length > limit ? value[..limit] : value;
}
