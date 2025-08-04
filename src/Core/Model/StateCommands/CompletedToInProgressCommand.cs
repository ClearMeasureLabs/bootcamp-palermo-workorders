namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;
public record CompletedToInProgressCommand(WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(WorkOrder, CurrentUser)
{
    public static string Name { get; set; } = "Re-Open";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Complete;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.InProgress;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Assignee || currentUser == WorkOrder.Creator; // TODO: Verify me!
    }

    public override string TransitionVerbPresentTense => Name;
    public override string TransitionVerbPastTense => "Re-Opened";
}
