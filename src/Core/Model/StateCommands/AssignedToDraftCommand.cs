namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record AssignedToDraftCommand(WorkOrder WorkOrder, Employee CurrentUser)
: StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Unassign";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Assigned;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Draft;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Unassigned";

    public override void Execute(StateCommandContext context)
    {
        WorkOrder.Assignee = null;
        WorkOrder.AssignedDate = null;
        base.Execute(context);
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Creator;
    }
}
