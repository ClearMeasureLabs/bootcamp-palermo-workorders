namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>
/// Command to cancel a work order from Assigned status.
/// Only the work order owner (Creator) can execute this command.
/// </summary>
public record AssignedToCancelledCommand(WorkOrder WorkOrder, Employee CurrentUser)
    : StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Cancel";

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

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Cancelled";
}
