using ClearMeasure.Bootcamp.UI.Shared.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Services;

public class WorkOrderSearchStateTests
{
    [Test]
    public void AssignedToMe_WhenNewlyConstructed_DefaultsToTrue()
    {
        var state = new WorkOrderSearchState();

        state.AssignedToMe.ShouldBeTrue();
    }
}
