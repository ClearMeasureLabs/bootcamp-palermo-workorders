using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class WorkOrderStatusBadgeCssTests
{
	[Test]
	public void CssClass_WhenDraft_ReturnsDraftClass()
	{
		WorkOrderStatusBadgeCss.CssClass(WorkOrderStatus.Draft)
			.ShouldBe("status-badge status-badge--draft");
	}

	[Test]
	public void CssClass_WhenAssigned_ReturnsAssignedClass()
	{
		WorkOrderStatusBadgeCss.CssClass(WorkOrderStatus.Assigned)
			.ShouldBe("status-badge status-badge--assigned");
	}

	[Test]
	public void CssClass_WhenInProgress_ReturnsInProgressClass()
	{
		WorkOrderStatusBadgeCss.CssClass(WorkOrderStatus.InProgress)
			.ShouldBe("status-badge status-badge--in-progress");
	}

	[Test]
	public void CssClass_WhenComplete_ReturnsCompleteClass()
	{
		WorkOrderStatusBadgeCss.CssClass(WorkOrderStatus.Complete)
			.ShouldBe("status-badge status-badge--complete");
	}

	[Test]
	public void CssClass_WhenCancelled_ReturnsCancelledClass()
	{
		WorkOrderStatusBadgeCss.CssClass(WorkOrderStatus.Cancelled)
			.ShouldBe("status-badge status-badge--cancelled");
	}

	[Test]
	public void CssClass_WhenNull_ReturnsMutedClass()
	{
		WorkOrderStatusBadgeCss.CssClass((WorkOrderStatus?)null)
			.ShouldBe("status-badge status-badge--muted");
	}

	[Test]
	public void CssClass_WhenUnknownStatusKey_ReturnsNeutralClass()
	{
		WorkOrderStatusBadgeCss.CssClass("ZZZ")
			.ShouldBe("status-badge status-badge--neutral");
	}
}
