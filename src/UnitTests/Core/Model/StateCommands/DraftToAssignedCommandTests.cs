using System.Text.Json;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

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

        var command = new DraftToAssignedCommand(order, employee);
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

        var command = new DraftToAssignedCommand(order, differentEmployee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new DraftToAssignedCommand(order, employee);
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

        var command = new DraftToAssignedCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Assigned));
        Assert.That(order.AssignedDate, Is.Not.Null);
    }

    [Test]
    public void ShouldBeValidWhenBeginStatusIsValueEqualToDraftEvenFromASeparateInstance()
    {
        var order = new WorkOrder();
        order.Status = DeserializeStatusCopyOf(WorkOrderStatus.Draft);
        var employee = new Employee();
        order.Creator = employee;

        var command = new DraftToAssignedCommand(order, employee);

        command.IsValid().ShouldBeTrue();
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new DraftToAssignedCommand(order, employee);
    }

    private static WorkOrderStatus DeserializeStatusCopyOf(WorkOrderStatus status)
    {
        var json = JsonSerializer.Serialize(status);
        return JsonSerializer.Deserialize<WorkOrderStatus>(json)!;
    }
}