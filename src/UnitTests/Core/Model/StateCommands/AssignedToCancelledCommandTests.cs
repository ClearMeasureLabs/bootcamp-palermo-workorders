using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class AssignedToCancelledCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new AssignedToCancelledCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Assigned;
        var creator = new Employee();
        var differentEmployee = new Employee();
        order.Creator = creator;

        var command = new AssignedToCancelledCommand(order, differentEmployee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidForAssignee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Assigned;
        var creator = new Employee();
        var assignee = new Employee();
        order.Creator = creator;
        order.Assignee = assignee;

        var command = new AssignedToCancelledCommand(order, assignee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValidForCreator()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Assigned;
        var creator = new Employee();
        order.Creator = creator;

        var command = new AssignedToCancelledCommand(order, creator);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.Assigned;
        var creator = new Employee();
        order.Creator = creator;

        var command = new AssignedToCancelledCommand(order, creator);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Cancelled));
    }

    [Test]
    public void ShouldHaveCorrectTransitionVerb()
    {
        var order = new WorkOrder();
        var employee = new Employee();

        var command = new AssignedToCancelledCommand(order, employee);

        Assert.That(command.TransitionVerbPresentTense, Is.EqualTo("Cancel"));
        Assert.That(command.TransitionVerbPastTense, Is.EqualTo("Cancelled"));
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new AssignedToCancelledCommand(order, employee);
    }
}
