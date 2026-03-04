using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderSubtaskTests
{
    [Test]
    public void WorkOrderSubtask_ShouldDefaultToNotCompleted()
    {
        var subtask = new WorkOrderSubtask { Title = "Test subtask" };

        subtask.IsCompleted.ShouldBeFalse();
    }

    [Test]
    public void WorkOrderSubtask_ShouldDefaultTitleToEmptyString()
    {
        var subtask = new WorkOrderSubtask();

        subtask.Title.ShouldBe(string.Empty);
    }

    [Test]
    public void AddSubtaskCommand_ShouldAddSubtaskToWorkOrder()
    {
        var workOrder = new WorkOrder { Id = Guid.NewGuid() };
        var subtask = new WorkOrderSubtask
        {
            Id = Guid.NewGuid(),
            WorkOrderId = workOrder.Id,
            Title = "Fix wiring",
            SortOrder = 0
        };

        workOrder.Subtasks.Add(subtask);

        workOrder.Subtasks.Count.ShouldBe(1);
        workOrder.Subtasks.First().Title.ShouldBe("Fix wiring");
    }

    [Test]
    public void ToggleSubtaskCommand_ShouldFlipIsCompleted()
    {
        var subtask = new WorkOrderSubtask { Id = Guid.NewGuid(), Title = "Task", IsCompleted = false };

        subtask.IsCompleted = !subtask.IsCompleted;
        subtask.IsCompleted.ShouldBeTrue();

        subtask.IsCompleted = !subtask.IsCompleted;
        subtask.IsCompleted.ShouldBeFalse();
    }

    [Test]
    public void RemoveSubtaskCommand_ShouldRemoveSubtask()
    {
        var workOrder = new WorkOrder { Id = Guid.NewGuid() };
        var subtask = new WorkOrderSubtask { Id = Guid.NewGuid(), WorkOrderId = workOrder.Id, Title = "Task" };
        workOrder.Subtasks.Add(subtask);

        workOrder.Subtasks.Remove(subtask);

        workOrder.Subtasks.Count.ShouldBe(0);
    }
}
