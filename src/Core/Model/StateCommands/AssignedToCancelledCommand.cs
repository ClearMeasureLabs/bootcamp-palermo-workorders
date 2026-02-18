namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record AssignedToCancelledCommand(Guid CorrelationId, WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(CorrelationId, WorkOrder, CurrentUser)
{
    public static string Name { get; set; } = "Cancel";
    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Assigned;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Cancelled;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Creator;
    }

    public override string TransitionVerbPresentTense { get; } = Name;
    public override string TransitionVerbPastTense { get; } = "Cancelled";
}