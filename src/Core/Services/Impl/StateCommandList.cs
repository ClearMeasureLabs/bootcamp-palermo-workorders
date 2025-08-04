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
        var commands = new List<IStateCommand>();
        commands.Add(new SaveDraftCommand(workOrder, currentUser));
        commands.Add(new DraftToAssignedCommand(workOrder, currentUser));
        commands.Add(new AssignedToCancelledCommand(workOrder, currentUser));
        commands.Add(new AssignedToInProgressCommand(workOrder, currentUser));
        commands.Add(new InProgressToCompleteCommand(workOrder, currentUser));
        commands.Add(new InProgressToAssigned(workOrder, currentUser));
        commands.Add(new CompleteToArchivedCommand(workOrder, currentUser));


        return commands.ToArray();
    }

    public IStateCommand GetMatchingCommand(WorkOrder order, Employee currentUser, string name)
    {
        var stateCommand = GetAllStateCommands(order, currentUser)
            .Single(command => command.Matches(name));
        return stateCommand;
    }
}