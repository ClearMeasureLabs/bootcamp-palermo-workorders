using ClearMeasure.Bootcamp.Core.Exceptions;
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
        if (GetBeginStatus() == WorkOrderStatus.Draft)
        {
            ValidateWorkOrder();
        }
        var currentUserFullName = CurrentUser.GetFullName();
        WorkOrder.ChangeStatus(CurrentUser, context.CurrentDateTime, GetEndStatus());
    }

    protected virtual void ValidateWorkOrder()
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(WorkOrder.Title))
        {
            errors["Title"] = new[] { "The Title field is required." };
        }

        if (string.IsNullOrWhiteSpace(WorkOrder.Description))
        {
            errors["Description"] = new[] { "The Description field is required." };
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}