namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record InProgressToAssigned(WorkOrder WorkOrder, Employee CurrentUser) : StateCommandBase(WorkOrder, CurrentUser)

{
    public static string Name { get; set; } = "Shelve";

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

    public override string TransitionVerbPresentTense { get; }
    public override string TransitionVerbPastTense { get; }
}
