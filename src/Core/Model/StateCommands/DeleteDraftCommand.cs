using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record DeleteDraftCommand(WorkOrder WorkOrder, Employee CurrentUser) :
    StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Delete";

    public override WorkOrderStatus GetBeginStatus()
    {
        return WorkOrderStatus.Draft;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.None;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        return true;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => string.Empty;

}