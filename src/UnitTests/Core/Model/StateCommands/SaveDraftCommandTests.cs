using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

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

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new SaveDraftCommand(order, employee);
    }

    [Test]
    public void Execute_WithMixedCaseDescription_ConvertsDescriptionToUpperCase()
    {
        var employee = new Employee();
        var order = new WorkOrder
        {
            Number = "456",
            Description = "Fix the leaky faucet in room 101",
            Status = WorkOrderStatus.Draft,
            Creator = employee
        };

        var command = new SaveDraftCommand(order, employee);
        command.Execute(new StateCommandContext());

        order.Description.ShouldBe("FIX THE LEAKY FAUCET IN ROOM 101");
    }

    [Test]
    public void Execute_WithNullDescription_ReturnsEmptyString()
    {
        var employee = new Employee();
        var order = new WorkOrder
        {
            Number = "789",
            Description = null,
            Status = WorkOrderStatus.Draft,
            Creator = employee
        };

        var command = new SaveDraftCommand(order, employee);
        command.Execute(new StateCommandContext());

        order.Description.ShouldBe(string.Empty);
    }

    [Test]
    public void Execute_WithMixedCaseTitle_ConvertsTitleToUpperCase()
    {
        var employee = new Employee();
        var order = new WorkOrder
        {
            Number = "456",
            Title = "Fix Leaky Faucet",
            Status = WorkOrderStatus.Draft,
            Creator = employee
        };

        var command = new SaveDraftCommand(order, employee);
        command.Execute(new StateCommandContext());

        order.Title.ShouldBe("FIX LEAKY FAUCET");
    }

    [Test]
    public void Execute_WithNullTitle_DoesNotThrow()
    {
        var employee = new Employee();
        var order = new WorkOrder
        {
            Number = "789",
            Title = null,
            Status = WorkOrderStatus.Draft,
            Creator = employee
        };

        var command = new SaveDraftCommand(order, employee);
        command.Execute(new StateCommandContext());

        order.Title.ShouldBeNull();
    }
}