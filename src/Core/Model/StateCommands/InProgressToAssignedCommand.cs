namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record InProgressToAssignedCommand(WorkOrder Order, Employee Employee) : StateCommandBase(Order, Employee)
{
    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.InProgress;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Assigned;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Assignee;
    }

    public const string Name = "Shelve";
    public override string TransitionVerbPresentTense => Name;
    public override string TransitionVerbPastTense => "Shelved";
}