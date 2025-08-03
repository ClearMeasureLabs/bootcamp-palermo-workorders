using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

public abstract class StateCommandBaseTester
{
    protected abstract StateCommandBase GetStateCommand(WorkOrder order, Employee employee);
}