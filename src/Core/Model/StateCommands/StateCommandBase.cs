using ClearMeasure.Bootcamp.Core.Model.Events;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.Core.Model.StateCommands;

public abstract record StateCommandBase(WorkOrder WorkOrder, Employee CurrentUser) : IStateCommand
{
    public abstract WorkOrderStatus GetBeginStatus();
    public abstract WorkOrderStatus GetEndStatus();
    protected abstract bool UserCanExecute(Employee currentUser);
    public abstract string TransitionVerbPresentTense { get; }
    public abstract string TransitionVerbPastTense { get; }

    public IStateTransitionEvent? StateTransitionEvent { get; protected set; }

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
        WorkOrder.ChangeStatus(CurrentUser, context.CurrentDateTime, GetEndStatus());
    }
}