using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;

namespace ClearMeasure.Bootcamp.Core.Services.Impl;

public class StateCommandList
{
    public IStateCommand[] GetValidStateCommands(WorkOrder workOrder, Employee currentUser)
    {
        var commands = new List<IStateCommand>(
            GetAllStateCommands(workOrder, currentUser));
        commands.RemoveAll(obj => !obj.IsValid());

        return commands.ToArray();
    }

    public virtual IStateCommand[] GetAllStateCommands(WorkOrder workOrder, Employee currentUser)
    {
        var commands = new List<IStateCommand>
        {
            new SaveDraftCommand(workOrder, currentUser),
            new DraftToAssignedCommand(workOrder, currentUser),
            new AssignedToInProgressCommand(workOrder, currentUser),
            new InProgressToCompleteCommand(workOrder, currentUser),
            new InProgressToAssignedCommand(workOrder, currentUser)
        };

        return commands.ToArray();
    }

    public IStateCommand GetMatchingCommand(WorkOrder order, Employee currentUser, string name)
    {
        var stateCommand = GetValidStateCommands(order, currentUser)
        .Single(command => command.Matches(name));
        return stateCommand;
    }
}