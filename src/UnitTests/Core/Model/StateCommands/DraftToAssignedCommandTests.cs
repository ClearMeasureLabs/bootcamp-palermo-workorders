using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class DraftToAssignedCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Complete;
        var employee = new Employee();
        order.Creator = employee;

        var command = new DraftToAssignedCommand(CorrelationId: Guid.NewGuid(), order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        var differentEmployee = new Employee();
        order.Assignee = employee;

        var command = new DraftToAssignedCommand(CorrelationId: Guid.NewGuid(), order, differentEmployee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new DraftToAssignedCommand(CorrelationId: Guid.NewGuid(), order, employee);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new DraftToAssignedCommand(CorrelationId: Guid.NewGuid(), order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Assigned));
        Assert.That(order.AssignedDate, Is.Not.Null);
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new DraftToAssignedCommand(CorrelationId: Guid.NewGuid(), order, employee);
    }
}