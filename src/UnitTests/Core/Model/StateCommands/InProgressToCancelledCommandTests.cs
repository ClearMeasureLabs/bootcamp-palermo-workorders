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
        var differentEmployee = new Employee();
        order.Creator = employee;

        var command = new InProgressToCancelledCommand(order, differentEmployee);
        command.IsValid().ShouldBeFalse();
    }

    [Test]
    public void ShouldBeValidForCreator()
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
    public void ShouldHaveCorrectTransitionVerbs()
    {
        var order = new WorkOrder();
        var employee = new Employee();

        var command = new InProgressToCancelledCommand(order, employee);

        command.TransitionVerbPresentTense.ShouldBe("Cancel");
        command.TransitionVerbPastTense.ShouldBe("Cancelled");
    }

    protected override StateCommandBase GetStateCommand(WorkOrder order, Employee employee)
    {
        return new InProgressToCancelledCommand(order, employee);
    }
}
