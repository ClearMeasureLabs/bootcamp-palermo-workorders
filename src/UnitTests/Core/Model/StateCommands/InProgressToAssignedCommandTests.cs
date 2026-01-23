using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class InProgressToAssignedCommandTests : StateCommandBaseTests
{
    [Test]
    public void ShouldNotBeValidInWrongStatus()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.Assigned;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new InProgressToAssignedCommand(order, employee);
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeValidWithWrongEmployee()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new InProgressToAssignedCommand(order, new Employee());
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldNotBeValidWhenCreatorTriesToShelve()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var creator = new Employee();
        var assignee = new Employee();
        order.Creator = creator;
        order.Assignee = assignee;

        var command = new InProgressToAssignedCommand(order, creator);
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldBeValid()
    {
        var order = new WorkOrder();
        order.Status = WorkOrderStatus.InProgress;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new InProgressToAssignedCommand(order, employee);
        command.IsValid().ShouldBeTrue();
    }

    [Test]
    public void ShouldTransitionStateProperly()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.InProgress;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new InProgressToAssignedCommand(order, employee);
        command.Execute(new StateCommandContext());

        order.Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public void ShouldCreateAuditEntry()
    {
        var order = new WorkOrder();
        order.Number = "123";
        order.Status = WorkOrderStatus.InProgress;
        var employee = new Employee();
        order.Assignee = employee;

        var command = new InProgressToAssignedCommand(order, employee);
        command.Execute(new StateCommandContext { CurrentDateTime = DateTime.Now });

        order.AuditEntries.Count.ShouldBe(1);
        order.AuditEntries[0].BeginStatus.ShouldBe(WorkOrderStatus.InProgress);
        order.AuditEntries[0].EndStatus.ShouldBe(WorkOrderStatus.Assigned);
        order.AuditEntries[0].Action.ShouldBe("Shelved");
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new InProgressToAssignedCommand(order, employee);
    }
}
