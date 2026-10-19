using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class CompleteToInProgressCommandTests
{
    [Test]
    public void ShouldChangeStatusFromCompleteToInProgress()
    {
        var creator = new Employee("creator", "First", "Last", "c@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Status = WorkOrderStatus.Complete,
            CompletedDate = new DateTime(2024, 12, 3, 15, 0, 0)
        };
        var command = new CompleteToInProgressCommand(workOrder, creator);

        command.Execute(new StateCommandContext { CurrentDateTime = DateTime.UtcNow });

        workOrder.Status.ShouldBe(WorkOrderStatus.InProgress);
    }

    [Test]
    public void ShouldClearCompletedDate()
    {
        var creator = new Employee("creator", "First", "Last", "c@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Status = WorkOrderStatus.Complete,
            CompletedDate = new DateTime(2024, 12, 3, 15, 0, 0)
        };
        var command = new CompleteToInProgressCommand(workOrder, creator);

        command.Execute(new StateCommandContext { CurrentDateTime = DateTime.UtcNow });

        workOrder.CompletedDate.ShouldBeNull();
    }

    [Test]
    public void ShouldAllowCreatorToExecute()
    {
        var creator = new Employee("creator", "First", "Last", "c@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Status = WorkOrderStatus.Complete
        };
        var command = new CompleteToInProgressCommand(workOrder, creator);

        command.IsValid().ShouldBeTrue();
    }

    [Test]
    public void ShouldRejectNonCreator()
    {
        var creator = new Employee("creator", "First", "Last", "c@test.com");
        var other = new Employee("other", "Other", "User", "o@test.com");
        var workOrder = new WorkOrder
        {
            Creator = creator,
            Status = WorkOrderStatus.Complete
        };
        var command = new CompleteToInProgressCommand(workOrder, other);

        command.IsValid().ShouldBeFalse();
    }
}
