using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model.StateCommands;

[TestFixture]
public class LastUpdatedDateStateCommandTests
{
    private static readonly DateTime CommandTime = new(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public void SaveDraftCommand_ShouldSetLastUpdatedDate()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.Draft, Creator = employee };

        new SaveDraftCommand(order, employee).Execute(new StateCommandContext { CurrentDateTime = CommandTime });

        order.LastUpdatedDate.ShouldBe(CommandTime);
    }

    [Test]
    public void DraftToAssignedCommand_ShouldSetLastUpdatedDate()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.Draft, Creator = employee };

        new DraftToAssignedCommand(order, employee).Execute(new StateCommandContext { CurrentDateTime = CommandTime });

        order.LastUpdatedDate.ShouldBe(CommandTime);
    }

    [Test]
    public void AssignedToInProgressCommand_ShouldSetLastUpdatedDate()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.Assigned, Assignee = employee };

        new AssignedToInProgressCommand(order, employee).Execute(new StateCommandContext { CurrentDateTime = CommandTime });

        order.LastUpdatedDate.ShouldBe(CommandTime);
    }

    [Test]
    public void InProgressToCompleteCommand_ShouldSetLastUpdatedDate()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };

        new InProgressToCompleteCommand(order, employee).Execute(new StateCommandContext { CurrentDateTime = CommandTime });

        order.LastUpdatedDate.ShouldBe(CommandTime);
    }

    [Test]
    public void InProgressToAssignedCommand_ShouldSetLastUpdatedDate()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.InProgress, Assignee = employee };

        new InProgressToAssignedCommand(order, employee).Execute(new StateCommandContext { CurrentDateTime = CommandTime });

        order.LastUpdatedDate.ShouldBe(CommandTime);
    }

    [Test]
    public void AssignedToCancelledCommand_ShouldSetLastUpdatedDate()
    {
        var employee = new Employee();
        var order = new WorkOrder { Status = WorkOrderStatus.Assigned, Creator = employee };

        new AssignedToCancelledCommand(order, employee).Execute(new StateCommandContext { CurrentDateTime = CommandTime });

        order.LastUpdatedDate.ShouldBe(CommandTime);
    }
}
