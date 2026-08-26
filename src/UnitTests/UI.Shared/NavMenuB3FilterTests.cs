using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class NavMenuB3FilterTests
{
    [Test]
    public void IsB3WorkOrderFilterActive_WhenMineAndNoAssigneeOrStatus_ReturnsTrue()
    {
        NavMenu.IsB3WorkOrderFilterActive(NavMenu.B3WorkOrderFilter.Mine, "workorder/search?Creator=tlovejoy")
            .ShouldBeTrue();
    }

    [Test]
    public void IsB3WorkOrderFilterActive_WhenMineAndAssigneePresent_ReturnsFalse()
    {
        NavMenu.IsB3WorkOrderFilterActive(NavMenu.B3WorkOrderFilter.Mine, "workorder/search?Assignee=gwillie")
            .ShouldBeFalse();
    }

    [Test]
    public void IsB3WorkOrderFilterActive_WhenAssignedToMeAndAssigneePresent_ReturnsTrue()
    {
        NavMenu.IsB3WorkOrderFilterActive(NavMenu.B3WorkOrderFilter.AssignedToMe, "workorder/search?Assignee=gwillie")
            .ShouldBeTrue();
    }

    [Test]
    public void IsB3WorkOrderFilterActive_WhenInProgressAndStatusMatches_ReturnsTrue()
    {
        NavMenu.IsB3WorkOrderFilterActive(
                NavMenu.B3WorkOrderFilter.InProgress,
                $"workorder/search?Status={WorkOrderStatus.InProgress.Key}")
            .ShouldBeTrue();
    }

    [Test]
    public void IsB3WorkOrderFilterActive_WhenInProgressAndStatusMissing_ReturnsFalse()
    {
        NavMenu.IsB3WorkOrderFilterActive(NavMenu.B3WorkOrderFilter.InProgress, "workorder/search")
            .ShouldBeFalse();
    }

    [Test]
    public void IsB3WorkOrderFilterActive_WhenUnknownFilter_ReturnsFalse()
    {
        NavMenu.IsB3WorkOrderFilterActive((NavMenu.B3WorkOrderFilter)99, "workorder/search")
            .ShouldBeFalse();
    }
}
