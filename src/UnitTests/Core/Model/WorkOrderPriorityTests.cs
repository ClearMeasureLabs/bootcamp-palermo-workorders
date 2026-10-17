using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderPriorityTests
{
    [Test]
    public void ShouldListAllPriorities()
    {
        var priorities = WorkOrderPriority.GetAllItems();

        priorities.Length.ShouldBe(3);
        priorities[0].ShouldBe(WorkOrderPriority.Low);
        priorities[1].ShouldBe(WorkOrderPriority.Normal);
        priorities[2].ShouldBe(WorkOrderPriority.High);
    }

    [Test]
    public void FromKey_ShouldReturnMatchingPriority()
    {
        WorkOrderPriority.FromKey("Low").ShouldBe(WorkOrderPriority.Low);
        WorkOrderPriority.FromKey("normal").ShouldBe(WorkOrderPriority.Normal);
        WorkOrderPriority.FromKey("HIGH").ShouldBe(WorkOrderPriority.High);
    }

    [Test]
    public void FromKey_WhenUnknown_ShouldThrow()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => WorkOrderPriority.FromKey("Urgent"));
    }
}
