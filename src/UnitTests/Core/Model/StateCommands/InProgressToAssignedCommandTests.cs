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
        var differentEmployee = new Employee();
        order.Assignee = employee;

        var command = new InProgressToAssignedCommand(order, differentEmployee);
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldBeValidForAssignee()
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
    public void ShouldHaveCorrectTransitionVerbs()
    {
        var order = new WorkOrder();
        var employee = new Employee();

        var command = new InProgressToAssignedCommand(order, employee);

        command.TransitionVerbPresentTense.ShouldBe("Shelve");
        command.TransitionVerbPastTense.ShouldBe("Shelved");
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new InProgressToAssignedCommand(order, employee);
    }
}
