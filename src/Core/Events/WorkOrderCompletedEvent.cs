using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Events;

/// <summary>
/// Event published when a work order is completed.
/// </summary>
public record WorkOrderCompletedEvent(WorkOrder WorkOrder) : INotification;
