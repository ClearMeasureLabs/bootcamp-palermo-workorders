namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record AssignedToInProgressCommand(Guid CorrelationId, WorkOrder WorkOrder, Employee CurrentUser)
    : StateCommandBase(CorrelationId, WorkOrder, CurrentUser)
{
    public const string Name = "Begin";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Assigned;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.InProgress;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Assignee;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Begun";
}