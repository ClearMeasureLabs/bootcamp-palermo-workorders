namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record UpdateDescriptionCommand(WorkOrder WorkOrder, Employee CurrentUser) :
    StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Save";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrder.Status;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrder.Status;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return currentUser == WorkOrder.Creator || currentUser == WorkOrder.Assignee;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Saved";
}
