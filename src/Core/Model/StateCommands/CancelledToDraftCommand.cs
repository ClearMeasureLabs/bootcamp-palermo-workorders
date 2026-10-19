using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record CancelledToDraftCommand(WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(WorkOrder,
    CurrentUser)
{
    public const string Name = "Restore";
    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Restored";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Cancelled;
    }

    protected override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Draft;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Creator;
    }

    public override void Execute(StateCommandContext context)
    {
        WorkOrder.AssignedDate = null;
        WorkOrder.Assignee = null;
        WorkOrder.CompletedDate = null;
        base.Execute(context);
    }
}
