using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class InProgressToCancelledCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Assigned;
        var employee = new Employee();
        order.Creator = employee;

        var command = new InProgressToCancelledCommand(order, employee);
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var employee = new Employee();
        order.Creator = employee;

        var command = new InProgressToCancelledCommand(order, new Employee());
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeValidWhenAssigneeTriesToCancel()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var creator = new Employee();
        var assignee = new Employee();
        order.Creator = creator;
        order.Assignee = assignee;

        var command = new InProgressToCancelledCommand(order, assignee);
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldBeValid()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var employee = new Employee();
        order.Creator = employee;

        var command = new InProgressToCancelledCommand(order, employee);
        command.IsValid().ShouldBeTrue();
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.InProgress;
        var employee = new Employee();
        order.Creator = employee;

        var command = new InProgressToCancelledCommand(order, employee);
        command.Execute(new StateCommandContext());

        order.Status.ShouldBe(WorkOrderStatus.Cancelled);
    }

    [Test]
    public void ShouldCreateAuditEntry()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.InProgress;
        var employee = new Employee();
        order.Creator = employee;

        var command = new InProgressToCancelledCommand(order, employee);
        command.Execute(new StateCommandContext { CurrentDateTime = DateTime.Now });

        order.AuditEntries.Count.ShouldBe(1);
        order.AuditEntries[0].BeginStatus.ShouldBe(WorkOrderStatus.InProgress);
        order.AuditEntries[0].EndStatus.ShouldBe(WorkOrderStatus.Cancelled);
        order.AuditEntries[0].Action.ShouldBe("Cancelled");
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new InProgressToCancelledCommand(order, employee);
    }
}
