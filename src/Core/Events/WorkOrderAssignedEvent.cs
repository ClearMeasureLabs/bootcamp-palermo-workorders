using ClearMeasure.Bootcamp.Core.Model;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Events;

/// <summary>
/// Event published when a work order is assigned to an employee.
/// </summary>
public record WorkOrderAssignedEvent(WorkOrder WorkOrder) : INotification;
