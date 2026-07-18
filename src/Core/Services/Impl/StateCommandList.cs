using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;

namespace ClearMeasure.Bootcamp.Core.Services.Impl;

public class StateCommandList
{
    public IStateCommand[] GetValidStateCommands(WorkRequest workRequest, Employee currentUser)
    {
        var commands = new List<IStateCommand>(
            GetAllStateCommands(workRequest, currentUser));
        commands.RemoveAll(obj => !obj.IsValid());

        return commands.ToArray();
    }

    public virtual IStateCommand[] GetAllStateCommands(WorkRequest workRequest, Employee currentUser)
    {
        var commands = new List<IStateCommand>();
        commands.Add(new SaveDraftCommand(workRequest, currentUser));
        commands.Add(new DraftToAssignedCommand(workRequest, currentUser));
        commands.Add(new AssignedToInProgressCommand(workRequest, currentUser));
        commands.Add(new InProgressToAssignedCommand(workRequest, currentUser));
        commands.Add(new InProgressToCompleteCommand(workRequest, currentUser));
        commands.Add(new AssignedToCancelledCommand(workRequest, currentUser));

        return commands.ToArray();
    }

    public IStateCommand GetMatchingCommand(WorkRequest order, Employee currentUser, string name)
    {
        var stateCommand = GetValidStateCommands(order, currentUser)
        .Single(command => command.Matches(name));
        return stateCommand;
    }
}