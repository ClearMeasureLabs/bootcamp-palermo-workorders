using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class WorkOrderStatusDisplayFormatterTests
{
    [Test]
    public void FormatForDisplay_Assigned_ReturnsAssigned()
    {
        WorkOrderStatusDisplayFormatter.FormatForDisplay(WorkOrderStatus.Assigned).ShouldBe("Assigned");
    }

    [Test]
    public void FormatForDisplay_Draft_ReturnsDraft()
    {
        WorkOrderStatusDisplayFormatter.FormatForDisplay(WorkOrderStatus.Draft).ShouldBe("Draft");
    }

    [Test]
    public void FormatForDisplay_InProgress_ReturnsInProgress()
    {
        WorkOrderStatusDisplayFormatter.FormatForDisplay(WorkOrderStatus.InProgress).ShouldBe("In Progress");
    }

    [Test]
    public void FormatForDisplay_Null_ReturnsEmpty()
    {
        WorkOrderStatusDisplayFormatter.FormatForDisplay(null).ShouldBe(string.Empty);
    }
}
