using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderFieldReadOnlyTests
{
    [Test]
    public void ShouldBeReadOnly_WhenStatusIsComplete()
    {
        var creator = new Employee("c", "A", "B", "a@b.com");
        var workOrder = new WorkOrder { Creator = creator, Status = WorkOrderStatus.Complete };
        IEnumerable<IStateCommand> commands = [new CompleteToInProgressCommand(workOrder, creator)];

        WorkOrderFieldReadOnly.Evaluate(workOrder, commands).ShouldBeTrue();
    }

    [Test]
    public void ShouldBeReadOnly_WhenStatusIsCancelled()
    {
        var workOrder = new WorkOrder { Status = WorkOrderStatus.Cancelled };

        WorkOrderFieldReadOnly.Evaluate(workOrder, Array.Empty<IStateCommand>()).ShouldBeTrue();
    }

    [Test]
    public void ShouldBeReadOnly_WhenNoValidCommands()
    {
        var workOrder = new WorkOrder { Status = WorkOrderStatus.Draft };

        WorkOrderFieldReadOnly.Evaluate(workOrder, Array.Empty<IStateCommand>()).ShouldBeTrue();
    }

    [Test]
    public void ShouldNotBeReadOnly_WhenInProgressWithCommands()
    {
        var assignee = new Employee("a", "A", "B", "a@b.com");
        var workOrder = new WorkOrder { Assignee = assignee, Status = WorkOrderStatus.InProgress };
        IEnumerable<IStateCommand> commands = [new InProgressToCompleteCommand(workOrder, assignee)];

        WorkOrderFieldReadOnly.Evaluate(workOrder, commands).ShouldBeFalse();
    }
}
