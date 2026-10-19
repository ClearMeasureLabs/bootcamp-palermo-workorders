using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderPriorityTests
{
    [Test]
    public void TryParse_ShouldReturnCanonicalValues_CaseInsensitive()
    {
        WorkOrderPriority.TryParse("low").ShouldBe(WorkOrderPriority.Low);
        WorkOrderPriority.TryParse("NORMAL").ShouldBe(WorkOrderPriority.Normal);
        WorkOrderPriority.TryParse(" High ").ShouldBe(WorkOrderPriority.High);
    }

    [Test]
    public void TryParse_ShouldReturnNull_WhenInvalid()
    {
        WorkOrderPriority.TryParse("urgent").ShouldBeNull();
        WorkOrderPriority.TryParse("").ShouldBeNull();
        WorkOrderPriority.TryParse(null).ShouldBeNull();
    }

    [Test]
    public void WorkOrder_ShouldDefaultPriorityToNormal_AndNormalizeInvalid()
    {
        var order = new WorkOrder();
        order.Priority.ShouldBe(WorkOrderPriority.Normal);
        order.Priority = "bogus-value";
        order.Priority.ShouldBe(WorkOrderPriority.Normal);
        order.Priority = "high";
        order.Priority.ShouldBe(WorkOrderPriority.High);
    }
}
