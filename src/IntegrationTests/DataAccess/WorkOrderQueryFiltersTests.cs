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
    public void ShouldApplyDueBeforeFilter_WhenDueBeforeIsSet()
    {
        var creator = new Employee("c", "c", "c", "c");
        var cutoff = new DateOnly(2026, 6, 1);
        var orders = new[]
        {
            new WorkOrder { Number = "1", Creator = creator, DueDate = new DateOnly(2026, 5, 31) },
            new WorkOrder { Number = "2", Creator = creator, DueDate = cutoff },
            new WorkOrder { Number = "3", Creator = creator, DueDate = new DateOnly(2026, 6, 2) },
            new WorkOrder { Number = "4", Creator = creator, DueDate = null }
        }.AsQueryable();

        var filtered = WorkOrderQueryFilters.Apply(orders, null, null, null, dueBefore: cutoff).ToArray();

        filtered.Length.ShouldBe(1);
        filtered[0].Number.ShouldBe("1");
    }
}
