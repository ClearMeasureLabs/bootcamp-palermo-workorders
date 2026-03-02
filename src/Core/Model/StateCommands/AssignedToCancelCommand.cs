namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record AssignedToCancelCommand(WorkOrder WorkOrder, Employee CurrentUser)
: StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Cancel";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Assigned;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Canceled;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Assignee;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Canceled";
}