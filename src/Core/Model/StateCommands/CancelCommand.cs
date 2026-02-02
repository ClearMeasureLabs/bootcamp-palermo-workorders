using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public record CancelCommand(WorkOrder WorkOrder, Employee CurrentUser) :
    StateCommandBase(WorkOrder, CurrentUser)
{
    public const string Name = "Cancel";

    public override WorkOrderStatus GetBeginStatus()
    {
        // Return the actual status of the work order for validation purposes
        return WorkOrder.Status;
    }

    public override WorkOrderStatus GetEndStatus()
    {
        return WorkOrderStatus.Cancelled;
    }

    protected override bool UserCanExecute(Employee currentUser)
    {
        // Only the creator (owner) can cancel
        return currentUser == WorkOrder.Creator;
    }

    public override string TransitionVerbPresentTense => Name;

    public override string TransitionVerbPastTense => "Cancelled";

    public override bool IsValid()
    {
        // Cancel is valid from Assigned or InProgress status
        var isValidBeginStatus = WorkOrder.Status == WorkOrderStatus.Assigned ||
                                  WorkOrder.Status == WorkOrderStatus.InProgress;
        var currentUserIsCorrectRole = UserCanExecute(CurrentUser);
        return isValidBeginStatus && currentUserIsCorrectRole;
    }
}
