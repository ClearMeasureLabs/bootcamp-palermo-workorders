using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class InProgressToAssignedCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Assigned;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new InProgressToAssignedCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var assignee = new Employee();
        var differentEmployee = new Employee();
        order.Assignee = assignee;

        var command = new InProgressToAssignedCommand(order, differentEmployee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidForCreator()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var creator = new Employee();
        var assignee = new Employee();
        order.Creator = creator;
        order.Assignee = assignee;

        var command = new InProgressToAssignedCommand(order, creator);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValidForAssignee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var assignee = new Employee();
        order.Assignee = assignee;

        var command = new InProgressToAssignedCommand(order, assignee);
        Assert.That(command.IsValid(), Is.True);
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

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Assigned));
    }

    [Test]
    public void ShouldHaveCorrectTransitionVerb()
    {
        var order = new WorkOrder();
        var employee = new Employee();

        var command = new InProgressToAssignedCommand(order, employee);

        Assert.That(command.TransitionVerbPresentTense, Is.EqualTo("Shelve"));
        Assert.That(command.TransitionVerbPastTense, Is.EqualTo("Shelved"));
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new InProgressToAssignedCommand(order, employee);
    }
}
