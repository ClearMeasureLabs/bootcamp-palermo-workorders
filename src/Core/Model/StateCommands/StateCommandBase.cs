using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public abstract record StateCommandBase(WorkOrder WorkOrder, Employee CurrentUser) : IStateCommand
{
    public abstract WorkOrderStatus GetBeginStatus();
    public abstract WorkOrderStatus GetEndStatus();
    protected abstract bool UserCanExecute(Employee currentUser);
    public abstract string TransitionVerbPresentTense { get; }
    public abstract string TransitionVerbPastTense { get; }

    public bool IsValid()
    {
        var beginStatusMatches = WorkOrder.Status == GetBeginStatus();
        var currentUserIsCorrectRole = UserCanExecute(CurrentUser);
        return beginStatusMatches && currentUserIsCorrectRole;
    }

    public bool Matches(string commandName)
    {
        return TransitionVerbPresentTense == commandName;
    }

    public virtual void Execute(StateCommandContext context)
    {
        var currentUserFullName = CurrentUser.GetFullName();
        var oldStatus = WorkOrder.Status.FriendlyName;
        WorkOrder.ChangeStatus(CurrentUser, context.CurrentDateTime, GetEndStatus());
        var newStatus = WorkOrder.Status.FriendlyName;

        // Record audit entry for status change
        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            WorkOrderId = WorkOrder.Id,
            UserName = CurrentUser.UserName,
            Timestamp = context.CurrentDateTime,
            Action = TransitionVerbPastTense,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Details = $"{currentUserFullName} {TransitionVerbPastTense.ToLower()} work order {WorkOrder.Number}"
        };
        context.AuditEntries.Add(auditEntry);
    }
}