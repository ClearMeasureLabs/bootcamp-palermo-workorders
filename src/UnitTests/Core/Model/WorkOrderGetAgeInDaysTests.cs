using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderGetAgeInDaysTests
{
    [Test]
    public void GetAgeInDays_WhenCreatedDateIsNull_ShouldReturnNull()
    {
        var workOrder = new WorkOrder();

        var age = workOrder.GetAgeInDays(TimeProvider.System);

        age.ShouldBeNull();
    }

    [Test]
    public void GetAgeInDays_WhenCreatedToday_ShouldReturnZero()
    {
        var today = TimeProvider.System.GetLocalNow().Date;
        var workOrder = new WorkOrder { CreatedDate = today };

        var age = workOrder.GetAgeInDays(TimeProvider.System);

        age.ShouldBe(0);
    }

    [Test]
    public void GetAgeInDays_WhenCreatedThreeDaysAgo_ShouldReturnThree()
    {
        var today = TimeProvider.System.GetLocalNow().Date;
        var workOrder = new WorkOrder { CreatedDate = today.AddDays(-3) };

        var age = workOrder.GetAgeInDays(TimeProvider.System);

        age.ShouldBe(3);
    }

    [Test]
    public void FormatAgeDisplay_WhenCreatedDateIsNull_ShouldReturnNull()
    {
        var workOrder = new WorkOrder();

        workOrder.FormatAgeDisplay(TimeProvider.System).ShouldBeNull();
    }

    [Test]
    public void FormatAgeDisplay_WhenCreatedToday_ShouldReturnToday()
    {
        var today = TimeProvider.System.GetLocalNow().Date;
        var workOrder = new WorkOrder { CreatedDate = today };

        workOrder.FormatAgeDisplay(TimeProvider.System).ShouldBe("Today");
    }

    [Test]
    public void FormatAgeDisplay_WhenCreatedOneDayAgo_ShouldReturnOneDay()
    {
        var today = TimeProvider.System.GetLocalNow().Date;
        var workOrder = new WorkOrder { CreatedDate = today.AddDays(-1) };

        workOrder.FormatAgeDisplay(TimeProvider.System).ShouldBe("1 day");
    }

    [Test]
    public void FormatAgeDisplay_WhenCreatedThreeDaysAgo_ShouldReturnThreeDays()
    {
        var today = TimeProvider.System.GetLocalNow().Date;
        var workOrder = new WorkOrder { CreatedDate = today.AddDays(-3) };

        workOrder.FormatAgeDisplay(TimeProvider.System).ShouldBe("3 days");
    }
}
