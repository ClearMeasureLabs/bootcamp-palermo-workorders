using ClearMeasure.Bootcamp.Core.Model;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record InProgressToAssignedCommand(WorkOrder Order, Employee Employee) : StateCommandBase(Order, Employee)
{
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

    public const string Name = "Shelve";
    public override string TransitionVerbPresentTense => Name;
    public override string TransitionVerbPastTense => "Shelved";
}