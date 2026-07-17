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

        Assert.That(commands, Is.Empty);
    }

    [Test]
    public void ShouldReturnAllStateCommandsInCorrectOrder()
    {
        var facilitator = new StateCommandList();
        var commands = facilitator.GetAllStateCommands(new WorkOrder(), new Employee());

        Assert.Multiple(() =>
        {
            Assert.That(commands, Has.Length.EqualTo(6));

            Assert.That(commands[0], Is.InstanceOf<SaveDraftCommand>());
            Assert.That(commands[1], Is.InstanceOf<DraftToAssignedCommand>());
            Assert.That(commands[2], Is.InstanceOf<AssignedToInProgressCommand>());
            Assert.That(commands[3], Is.InstanceOf<InProgressToAssignedCommand>());
            Assert.That(commands[4], Is.InstanceOf<InProgressToCompleteCommand>());
            Assert.That(commands[5], Is.InstanceOf<AssignedToCancelledCommand>());
        });
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

        Assert.That(commands, Has.Length.EqualTo(2));
    }

    [Test]
    public void ShouldGetValidMatchingCommands()
    {
        var workOrder = new WorkOrder();
        var employee = new Employee();
        workOrder.Creator = employee;
        var stubFacilitator = new StubStateCommandList();
        var expected = new StubbedStateCommand(true)
        {
            TransitionVerbPresentTense = "Save",
        };

        var commandsToReturn = new IStateCommand[]
        {
            expected,
            new StubbedStateCommand(true),
            new StubbedStateCommand(false)
        };

        stubFacilitator.CommandsToReturn = commandsToReturn;

        var commands = stubFacilitator.GetMatchingCommand(workOrder, employee, "Save");

        Assert.That(commands, Is.SameAs(expected));
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

        public string TransitionVerbPresentTense { get; set; } = String.Empty;

        public bool Matches(string commandName)
        {
            return TransitionVerbPresentTense == commandName;
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