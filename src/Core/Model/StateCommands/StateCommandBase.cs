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

    /// <summary>
    /// Validates the work order fields and returns a list of validation errors.
    /// </summary>
    /// <returns>A list of validation error messages. Empty if validation passes.</returns>
    public virtual IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(WorkOrder.Title))
        {
            errors.Add("Title is required.");
        }

        if (string.IsNullOrWhiteSpace(WorkOrder.Description))
        {
            errors.Add("Description is required.");
        }

        return errors;
    }

    public virtual void Execute(StateCommandContext context)
    {
        var currentUserFullName = CurrentUser.GetFullName();
        WorkOrder.ChangeStatus(CurrentUser, context.CurrentDateTime, GetEndStatus());
    }
}