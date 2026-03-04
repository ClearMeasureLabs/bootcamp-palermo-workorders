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
    public void Execute_WithLowercaseTitle_ShouldConvertToUppercase()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.Draft;
        order.Title = "fix leaky faucet";
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Title, Is.EqualTo("FIX LEAKY FAUCET"));
    }

    [Test]
    public void Execute_WithMixedCaseTitle_ShouldConvertToUppercase()
    {
        var order = new WorkOrder();
        order.Number = "456";
        order.Status = WorkOrderStatus.Draft;
        order.Title = "Replace HVAC Filter";
        var employee = new Employee();
        order.Creator = employee;

        var command = new SaveDraftCommand(order, employee);
        command.Execute(new StateCommandContext());

        Assert.That(order.Title, Is.EqualTo("REPLACE HVAC FILTER"));
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new SaveDraftCommand(order, employee);
    }
}