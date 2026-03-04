using ClearMeasure.Bootcamp.Core;
using MediatR;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record ReassignWorkOrderCommand(WorkOrder WorkOrder, Employee NewAssignee, Employee RequestedBy)
    : IRequest<StateCommandResult>, IRemotableRequest
{
    public const string Name = "Reassign";

    public bool IsValid()
    {
        var statusValid = WorkOrder.Status == WorkOrderStatus.Assigned || WorkOrder.Status == WorkOrderStatus.InProgress;
        var requesterIsCreator = WorkOrder.Creator?.Id == RequestedBy.Id;
        var newAssigneeCanFulfill = NewAssignee.CanFulfilWorkOrder();
        return statusValid && requesterIsCreator && newAssigneeCanFulfill;
    }

    public void Execute()
    {
        WorkOrder.Assignee = NewAssignee;
        WorkOrder.Status = WorkOrderStatus.Assigned;
    }
}
