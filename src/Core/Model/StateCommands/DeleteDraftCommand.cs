using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record DeleteDraftCommand(WorkOrder WorkOrder, Employee CurrentUser) :
    StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Delete";

    public override WorkOrderStatus GetBeginStatus()
    {
        throw new NotImplementedException();
    }

    public override WorkOrderStatus GetEndStatus()
    {
        throw new NotImplementedException();
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        throw new NotImplementedException();
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => string.Empty;

    public override void Execute(StateCommandContext context)
    {
        throw new NotImplementedException();

    }
}