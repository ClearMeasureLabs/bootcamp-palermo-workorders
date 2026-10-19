namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record DraftToCancelledCommand(WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(WorkOrder,
    CurrentUser)
{
    private const string Name = "Discard";
    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Discarded";

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
}
