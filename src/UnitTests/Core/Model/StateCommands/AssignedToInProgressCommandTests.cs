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
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new AssignedToInProgressCommand(CorrelationId: Guid.NewGuid(), order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Assigned;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new AssignedToInProgressCommand(CorrelationId: Guid.NewGuid(), order, new Employee());
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Assigned;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new AssignedToInProgressCommand(CorrelationId: Guid.NewGuid(), order, employee);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.Assigned;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new AssignedToInProgressCommand(CorrelationId: Guid.NewGuid(), order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.InProgress));
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new AssignedToInProgressCommand(CorrelationId: Guid.NewGuid(), order, employee);
    }
}