using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class AssignedToInProgressCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var order = new WorkRequest();
        order.Status = WorkRequestStatus.Draft;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new AssignedToInProgressCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkRequest();
        order.Status = WorkRequestStatus.Assigned;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new AssignedToInProgressCommand(order, new Employee());
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var order = new WorkRequest();
        order.Status = WorkRequestStatus.Assigned;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new AssignedToInProgressCommand(order, employee);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var order = new WorkRequest();
        order.Number = "123";
        order.Status = WorkRequestStatus.Assigned;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new AssignedToInProgressCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkRequestStatus.InProgress));
    }

    protected override StateCommandBase GetStateCommand(WorkRequest order, Employee employee)
    {
        return new AssignedToInProgressCommand(order, employee);
    }
}