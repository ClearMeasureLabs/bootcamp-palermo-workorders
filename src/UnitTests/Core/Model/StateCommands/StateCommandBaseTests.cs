using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

public abstract class StateCommandBaseTests
{
    // ReSharper disable once UnusedMember.Global -- overridden by all concrete StateCommand test subclasses
    protected abstract StateCommandBase GetStateCommand(WorkOrder order, Employee employee);
}