using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record DraftToCancelledCommand(WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(WorkOrder,
    CurrentUser)
{
    private const string Name = "Cancel";
    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Cancelled";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Draft;
    }

    protected override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Cancelled;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Creator;
    }

    public override void Execute(StateCommandContext context)
    {
        WorkOrder.AssignedDate = null;
        WorkOrder.Assignee = null;
        base.Execute(context);
    }
}
