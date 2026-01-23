using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class InProgressToCancelledCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var creator = new Employee();
        order.Creator = creator;

        var command = new InProgressToCancelledCommand(order, creator);
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var creator = new Employee();
        order.Creator = creator;

        var command = new InProgressToCancelledCommand(order, new Employee());
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeValidWhenAssigneeTriesToCancel()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var creator = new Employee();
        var assignee = new Employee();
        order.Creator = creator;
        order.Assignee = assignee;

        var command = new InProgressToCancelledCommand(order, assignee);
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldBeValidWhenCreatorCancels()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var creator = new Employee();
        order.Creator = creator;

        var command = new InProgressToCancelledCommand(order, creator);
        command.IsValid().ShouldBeTrue();
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.InProgress;
        var creator = new Employee();
        order.Creator = creator;

        var command = new InProgressToCancelledCommand(order, creator);
        command.Execute(new StateCommandContext());

        order.Status.ShouldBe(WorkOrderStatus.Cancelled);
    }

    [Test]
    public void ShouldHaveCorrectCommandName()
    {
        var order = new WorkOrder();
        var employee = new Employee();

        var command = new InProgressToCancelledCommand(order, employee);

        command.TransitionVerbPresentTense.ShouldBe("Cancel");
        command.TransitionVerbPastTense.ShouldBe("Cancelled");
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new InProgressToCancelledCommand(order, employee);
    }
}
