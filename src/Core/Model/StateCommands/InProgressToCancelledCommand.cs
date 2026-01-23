namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

/// <summary>
/// Command to cancel a work order that is currently in InProgress status.
/// Only the work order owner (creator) can execute this command.
/// </summary>
public record InProgressToCancelledCommand(WorkOrder WorkOrder, Employee CurrentUser)
    : StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Cancel";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.InProgress;
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
