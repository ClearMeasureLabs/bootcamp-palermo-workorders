using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;

namespace ClearMeasure.Bootcamp.Core.Services.Impl;

public class StateCommandList
{
    public IStateCommand[] GetValidStateCommands(Guid correlationId, WorkOrder workOrder, Employee currentUser)
    {
        var commands = new List<IStateCommand>(
            GetAllStateCommands(correlationId, workOrder, currentUser));
        commands.RemoveAll(obj => !obj.IsValid());

        return commands.ToArray();
    }

    public virtual IStateCommand[] GetAllStateCommands(Guid correlationId, WorkOrder workOrder, Employee currentUser)
    {
        var commands = new List<IStateCommand>();
        commands.Add(new InProgressToCancelledCommand(correlationId, workOrder, currentUser));
        commands.Add(new SaveDraftCommand(correlationId, workOrder, currentUser));
        commands.Add(new DraftToAssignedCommand(correlationId, workOrder, currentUser));
        commands.Add(new AssignedToCancelledCommand(correlationId, workOrder, currentUser));
        commands.Add(new AssignedToInProgressCommand(correlationId, workOrder, currentUser));
        commands.Add(new InProgressToCompleteCommand(correlationId, workOrder, currentUser));
        commands.Add(new InProgressToAssigned(correlationId, workOrder, currentUser));

        return commands.ToArray();
    }

    public IStateCommand GetMatchingCommand(Guid correlationId, WorkOrder order, Employee currentUser, string name)
    {
        var stateCommand = GetValidStateCommands(correlationId, order, currentUser)
        .Single(command => command.Matches(name));
        return stateCommand;
    }
}