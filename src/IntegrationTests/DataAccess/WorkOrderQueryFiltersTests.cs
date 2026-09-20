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
    public void ShouldExcludeCancelled_WhenStatusIsNull()
    {
        var orders = new WorkOrder[]
        {
            new() { Status = WorkOrderStatus.Draft },
            new() { Status = WorkOrderStatus.Assigned },
            new() { Status = WorkOrderStatus.InProgress },
            new() { Status = WorkOrderStatus.Complete },
            new() { Status = WorkOrderStatus.Cancelled }
        };

        var result = WorkOrderQueryFilters.Apply(orders.AsQueryable(), null, null, null).ToList();

        result.ShouldNotContain(o => o.Status == WorkOrderStatus.Cancelled);
        result.Count.ShouldBe(4);
    }

    [Test]
    public void ShouldIncludeCancelled_WhenStatusIsCancelled()
    {
        var orders = new WorkOrder[]
        {
            new() { Status = WorkOrderStatus.Draft },
            new() { Status = WorkOrderStatus.Cancelled },
            new() { Status = WorkOrderStatus.Cancelled }
        };

        var result = WorkOrderQueryFilters.Apply(orders.AsQueryable(), null, null, WorkOrderStatus.Cancelled).ToList();

        result.ShouldAllBe(o => o.Status == WorkOrderStatus.Cancelled);
        result.Count.ShouldBe(2);
    }

    [Test]
    public void ShouldExcludeCancelled_WhenOtherStatusFilterSet()
    {
        var orders = new WorkOrder[]
        {
            new() { Status = WorkOrderStatus.Draft },
            new() { Status = WorkOrderStatus.Assigned },
            new() { Status = WorkOrderStatus.Cancelled }
        };

        var result = WorkOrderQueryFilters.Apply(orders.AsQueryable(), null, null, WorkOrderStatus.Assigned).ToList();

        result.ShouldAllBe(o => o.Status == WorkOrderStatus.Assigned);
        result.Count.ShouldBe(1);
    }

    [Test]
    public void ShouldExcludeCancelled_WhenNoFiltersAtAll()
    {
        var orders = new WorkOrder[]
        {
            new() { Status = WorkOrderStatus.Draft },
            new() { Status = WorkOrderStatus.Assigned },
            new() { Status = WorkOrderStatus.InProgress },
            new() { Status = WorkOrderStatus.Complete },
            new() { Status = WorkOrderStatus.Cancelled }
        };

        var result = WorkOrderQueryFilters.Apply(orders.AsQueryable(), null, null, null).ToList();

        result.ShouldNotContain(o => o.Status == WorkOrderStatus.Cancelled);
        result.Count.ShouldBe(4);
    }
}
