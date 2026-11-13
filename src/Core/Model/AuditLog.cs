namespace ClearMeasure.Bootcamp.Core.Model;

/// <summary>Immutable historical record of an application operation.</summary>
public sealed class AuditLog
{
    /// <summary>Database-generated sequence.</summary>
    public long Id { get; private set; }
    /// <summary>Related work order, when applicable.</summary>
    public Guid? WorkOrderId { get; init; }
    /// <summary>Operation or transition name.</summary>
    public string Action { get; init; } = "";
    /// <summary>Type of the affected entity.</summary>
    public string EntityType { get; init; } = "";
    /// <summary>Identifier of the affected entity.</summary>
    public Guid? EntityId { get; init; }
    /// <summary>Actor identity, or an explicit system/anonymous identity.</summary>
    public string ActorUserName { get; init; } = "system";
    /// <summary>Actor display name, if known.</summary>
    public string? ActorDisplayName { get; init; }
    /// <summary>JSON snapshot of previous changed values.</summary>
    public string? PreviousValue { get; init; }
    /// <summary>JSON snapshot of new changed values.</summary>
    public string? NewValue { get; init; }
    /// <summary>UTC occurrence time.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    /// <summary>Remote address captured at the server boundary.</summary>
    public string? IpAddress { get; init; }
    /// <summary>Client user agent captured at the server boundary.</summary>
    public string? UserAgent { get; init; }
    /// <summary>Request or trace correlation identifier.</summary>
    public string? CorrelationId { get; init; }
}
