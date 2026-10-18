using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

public class WorkOrderSearchModelSearchFiltersTests
{
    [Test]
    public void Clear_ShouldResetAllFilters()
    {
        var filters = new WorkOrderSearchModel.SearchFilters
        {
            Creator = "alice",
            Assignee = "bob",
            Status = "Assigned",
            OverdueOnly = true
        };

        filters.Clear();

        filters.Creator.ShouldBeNull();
        filters.Assignee.ShouldBeNull();
        filters.Status.ShouldBeNull();
        filters.OverdueOnly.ShouldBeFalse();
    }
}
