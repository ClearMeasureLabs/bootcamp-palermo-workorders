using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class CompleteToAssignedCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new CompleteToAssignedCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Complete;
        var creator = new Employee();
        var differentEmployee = new Employee();
        order.Creator = creator;

        var command = new CompleteToAssignedCommand(order, differentEmployee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Complete;
        var employee = new Employee();
        order.Creator = employee;

        var command = new CompleteToAssignedCommand(order, employee);
        Assert.That(command.IsValid(), Is.True);
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.Complete;
        order.CompletedDate = DateTime.Now.AddDays(-1);
        var employee = new Employee();
        order.Creator = employee;

        var command = new CompleteToAssignedCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Assigned));
        Assert.That(order.CompletedDate, Is.Null);
        Assert.That(order.AssignedDate, Is.Not.Null);
    }

    [Test]
    public void ShouldClearCompletedDateWhenReassigning()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.Complete;
        order.CompletedDate = new DateTime(2025, 1, 15);
        var employee = new Employee();
        order.Creator = employee;

        var command = new CompleteToAssignedCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.CompletedDate, Is.Null);
    }

    [Test]
    public void ShouldUpdateAssignedDateWhenReassigning()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.Complete;
        order.CompletedDate = new DateTime(2025, 1, 15);
        order.AssignedDate = new DateTime(2025, 1, 10);
        var employee = new Employee();
        order.Creator = employee;

        var context = new StateCommandContext { CurrentDateTime = new DateTime(2025, 2, 1) };
        var command = new CompleteToAssignedCommand(order, employee);
        command.Execute(context);

        Assert.That(order.AssignedDate, Is.EqualTo(new DateTime(2025, 2, 1)));
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new CompleteToAssignedCommand(order, employee);
    }
}
