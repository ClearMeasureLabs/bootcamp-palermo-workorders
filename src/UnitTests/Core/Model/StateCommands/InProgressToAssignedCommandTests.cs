using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class InProgressToAssignedCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var assignee = new Employee();
        order.Assignee = assignee;

        var command = new InProgressToAssignedCommand(order, assignee);
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var assignee = new Employee();
        order.Assignee = assignee;

        var command = new InProgressToAssignedCommand(order, new Employee());
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeValidWhenCreatorTriesToShelve()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var creator = new Employee();
        var assignee = new Employee();
        order.Creator = creator;
        order.Assignee = assignee;

        var command = new InProgressToAssignedCommand(order, creator);
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldBeValidWhenAssigneeShelves()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var assignee = new Employee();
        order.Assignee = assignee;

        var command = new InProgressToAssignedCommand(order, assignee);
        command.IsValid().ShouldBeTrue();
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.InProgress;
        var assignee = new Employee();
        order.Assignee = assignee;

        var command = new InProgressToAssignedCommand(order, assignee);
        command.Execute(new StateCommandContext());

        order.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public void ShouldHaveCorrectCommandName()
    {
        var order = new WorkOrder();
        var employee = new Employee();

        var command = new InProgressToAssignedCommand(order, employee);

        command.TransitionVerbPresentTense.ShouldBe("Shelve");
        command.TransitionVerbPastTense.ShouldBe("Shelved");
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new InProgressToAssignedCommand(order, employee);
    }
}
