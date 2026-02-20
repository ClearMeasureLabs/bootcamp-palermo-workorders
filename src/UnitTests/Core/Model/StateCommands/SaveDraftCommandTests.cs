using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class SaveDraftCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Complete;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, new Employee());
        Assert.That(command.IsValid(), Is.False);
    }

    [Test]
    public void ShouldBeValid()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Draft;
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
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

        var command = new SaveDraftCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Status, Is.EqualTo(WorkOrderStatus.Draft));
        Assert.That(order.CreatedDate, Is.Not.Null);
    }

    [Test]
    public void Execute_TitleExceeds250Characters_ThrowsArgumentException()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.Draft;
        order.Title = new string('A', 251);
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        var ex = Assert.Throws<ArgumentException>(() => command.Execute(new StateCommandContext()));
        
        Assert.That(ex!.Message, Is.EqualTo("Title cannot exceed 250 characters"));
    }

    [Test]
    public void Execute_DescriptionExceeds500Characters_ThrowsArgumentException()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.Draft;
        order.Title = "Valid title";
        order.Description = new string('B', 501);
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        var ex = Assert.Throws<ArgumentException>(() => command.Execute(new StateCommandContext()));
        
        Assert.That(ex!.Message, Is.EqualTo("Description cannot exceed 500 characters"));
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new SaveDraftCommand(order, employee);
    }
}