using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.Core.Services.Impl;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class StateCommandListTests
{
    [Test]
    public void ShouldGetNoValidStateCommandsForWrongUser()
    {
        var facilitator = new StateCommandList();
        var workOrder = new WorkOrder();
        var employee = new Employee();
        var commands = facilitator.GetValidStateCommands(workOrder, employee);

        Assert.That(commands.Length, Is.EqualTo(0));
    }

    [Test]
    public void ShouldReturnAllStateCommandsInCorrectOrder()
    {
        var facilitator = new StateCommandList();
        var commands = facilitator.GetAllStateCommands(new WorkOrder(), new Employee());

        Assert.That(commands.Length, Is.EqualTo(7));

        Assert.That(commands[0], Is.InstanceOf(typeof(SaveDraftCommand)));
        Assert.That(commands[1], Is.InstanceOf(typeof(DraftToAssignedCommand)));
        Assert.That(commands[2], Is.InstanceOf(typeof(AssignedToCancelledCommand)));
        Assert.That(commands[3], Is.InstanceOf(typeof(AssignedToInProgressCommand)));
        Assert.That(commands[4], Is.InstanceOf(typeof(InProgressToCompleteCommand)));
        Assert.That(commands[5], Is.InstanceOf(typeof(InProgressToAssigned)));
        Assert.That(commands[6], Is.InstanceOf(typeof(CompletedToInProgressCommand)));
    }

    [Test]
    public void ShouldFilterFullListToReturnValidCommands()
    {
        var stubFacilitator = new StubStateCommandList();
        var commandsToReturn = new IStateCommand[]
        {
            new StubbedStateCommand(true),
            new StubbedStateCommand(true),
            new StubbedStateCommand(false)
        };

        stubFacilitator.CommandsToReturn = commandsToReturn;

        var commands = stubFacilitator.GetValidStateCommands(null!, null!);

        Assert.That(commands.Length, Is.EqualTo(2));
    }

    public class StubStateCommandList : StateCommandList
    {
        public IStateCommand[] CommandsToReturn { get; set; } = null!;

        public override IStateCommand[] GetAllStateCommands(WorkOrder workOrder, Employee employee)
        {
            return CommandsToReturn;
        }
    }

    public class StubbedStateCommand(bool isValid) : IStateCommand
    {
        public bool IsValid()
        {
            return isValid;
        }

        public string TransitionVerbPresentTense => throw new NotImplementedException();

        public bool Matches(string commandName)
        {
            throw new NotImplementedException();
        }

        public WorkOrderStatus GetBeginStatus()
        {
            throw new NotImplementedException();
        }

        public void Execute(StateCommandContext context)
        {
            throw new NotImplementedException();
        }
    }
}