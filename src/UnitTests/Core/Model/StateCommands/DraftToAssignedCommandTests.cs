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
        order.Title = "Test Title";
        order.Description = "Test Description";
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new DraftToAssignedCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Assigned));
        Assert.That(order.AssignedDate, Is.Not.Null);
    }

    [Test]
    public void ShouldThrowWhenTitleIsNull()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Title = null;
        order.Description = "Test Description";
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new DraftToAssignedCommand(order, employee);
        var ex = Assert.Throws<InvalidOperationException>(() => command.Execute(new StateCommandContext()));
        Assert.That(ex?.Message, Is.EqualTo("Title cannot be empty"));
    }

    [Test]
    public void ShouldThrowWhenTitleIsEmpty()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Title = "";
        order.Description = "Test Description";
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new DraftToAssignedCommand(order, employee);
        var ex = Assert.Throws<InvalidOperationException>(() => command.Execute(new StateCommandContext()));
        Assert.That(ex?.Message, Is.EqualTo("Title cannot be empty"));
    }

    [Test]
    public void ShouldThrowWhenDescriptionIsNull()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Title = "Test Title";
        order.Description = null;
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new DraftToAssignedCommand(order, employee);
        var ex = Assert.Throws<InvalidOperationException>(() => command.Execute(new StateCommandContext()));
        Assert.That(ex?.Message, Is.EqualTo("Description cannot be empty"));
    }

    [Test]
    public void ShouldThrowWhenDescriptionIsEmpty()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Title = "Test Title";
        order.Description = "";
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new DraftToAssignedCommand(order, employee);
        var ex = Assert.Throws<InvalidOperationException>(() => command.Execute(new StateCommandContext()));
        Assert.That(ex?.Message, Is.EqualTo("Description cannot be empty"));
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new DraftToAssignedCommand(order, employee);
    }
}