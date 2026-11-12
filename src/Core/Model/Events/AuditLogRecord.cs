using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.Events;

/// <summary>
/// Records an auditable event reason for application subscribers.
/// </summary>
public record AuditLogRecord(string Reason, string ToolName) : INotification;
