using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.DataAccess.Handlers;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.DataAccess;

[TestFixture]
public class WorkOrderQueryFiltersTests
{
    [Test]
    public void ShouldApplyAllFilters_WhenSpecificationPopulated()
    {
        var assignee = new Employee("a", "a", "a", "a");
        var creator = new Employee("c", "c", "c", "c");
        var orders = new[]
        {
            new WorkOrder { Number = "1", Assignee = assignee, Creator = creator, Status = WorkOrderStatus.Assigned },
            new WorkOrder { Number = "2", Assignee = assignee, Creator = creator, Status = WorkOrderStatus.Draft }
        }.AsQueryable();

        var filtered = WorkOrderQueryFilters.Apply(orders, assignee, creator, WorkOrderStatus.Assigned).ToArray();

        filtered.Length.ShouldBe(1);
        filtered[0].Number.ShouldBe("1");
    }

    [Test]
    public void DefaultSearchExcludesCancelled()
    {
        var orders = new[]
        {
            new WorkOrder { Number = "1", Status = WorkOrderStatus.Draft },
            new WorkOrder { Number = "2", Status = WorkOrderStatus.Assigned },
            new WorkOrder { Number = "3", Status = WorkOrderStatus.Cancelled }
        }.AsQueryable();

        var filtered = WorkOrderQueryFilters.Apply(orders, null, null, null).ToArray();

        filtered.Length.ShouldBe(2);
        filtered.ShouldNotContain(o => o.Status == WorkOrderStatus.Cancelled);
    }

    [Test]
    public void ExplicitCancelledStatusIncludesCancelled()
    {
        var orders = new[]
        {
            new WorkOrder { Number = "1", Status = WorkOrderStatus.Draft },
            new WorkOrder { Number = "2", Status = WorkOrderStatus.Cancelled }
        }.AsQueryable();

        var filtered = WorkOrderQueryFilters.Apply(orders, null, null, WorkOrderStatus.Cancelled).ToArray();

        filtered.Length.ShouldBe(1);
        filtered[0].Status.ShouldBe(WorkOrderStatus.Cancelled);
    }

    [Test]
    public void OverdueFilterStillExcludesCancelled()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var orders = new[]
        {
            new WorkOrder { Number = "1", Status = WorkOrderStatus.Assigned, DueDate = yesterday },
            new WorkOrder { Number = "2", Status = WorkOrderStatus.Cancelled, DueDate = yesterday }
        }.AsQueryable();

        var filtered = WorkOrderQueryFilters.Apply(orders, null, null, null, true).ToArray();

        filtered.Length.ShouldBe(1);
        filtered[0].Status.ShouldBe(WorkOrderStatus.Assigned);
    }

    [Test]
    public void AssigneeAndCreatorFiltersUnaffectedByDefaultCancelledExclusion()
    {
        var assignee = new Employee("a", "a", "a", "a");
        var creator = new Employee("c", "c", "c", "c");
        var orders = new[]
        {
            new WorkOrder { Number = "1", Assignee = assignee, Creator = creator, Status = WorkOrderStatus.Assigned },
            new WorkOrder { Number = "2", Assignee = assignee, Creator = creator, Status = WorkOrderStatus.Cancelled },
            new WorkOrder { Number = "3", Assignee = assignee, Creator = creator, Status = WorkOrderStatus.Draft }
        }.AsQueryable();

        var filtered = WorkOrderQueryFilters.Apply(orders, assignee, creator, null).ToArray();

        filtered.Length.ShouldBe(2);
        filtered.ShouldNotContain(o => o.Status == WorkOrderStatus.Cancelled);
    }
}
