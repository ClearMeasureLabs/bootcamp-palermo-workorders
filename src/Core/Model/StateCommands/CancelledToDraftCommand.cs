namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record CancelledToDraftCommand(WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(WorkOrder, CurrentUser)
{
    public static string Name { get; set; } = "Cancel";
    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Assigned;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Draft;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Creator;
    }

    public override string TransitionVerbPresentTense { get; } = Name;
    public override string TransitionVerbPastTense { get; } = "Cancelled";
}