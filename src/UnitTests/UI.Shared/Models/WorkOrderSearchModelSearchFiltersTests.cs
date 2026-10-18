using ClearMeasure.Bootcamp.UI.Shared.Models;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Models;

public class WorkOrderSearchModelSearchFiltersTests
{
    [Test]
    public void HasAny_NoFilters_ShouldBeFalse()
    {
        var filters = new WorkOrderSearchModel.SearchFilters();

        filters.HasAny.ShouldBeFalse();
    }

    [Test]
    public void HasAny_OnlyStatusSet_ShouldBeTrue()
    {
        var filters = new WorkOrderSearchModel.SearchFilters { Status = "Draft" };

        filters.HasAny.ShouldBeTrue();
    }

    [Test]
    public void HasAny_OnlyOverdueOnly_ShouldBeTrue()
    {
        var filters = new WorkOrderSearchModel.SearchFilters { OverdueOnly = true };

        filters.HasAny.ShouldBeTrue();
    }
}
