using ClearMeasure.Bootcamp.Core.Model;
using NUnit.Framework;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Model;

[TestFixture]
public class WorkOrderGetContactEmailTests
{
    [Test]
    public void GetContactEmail_WhenCreatorHasEmail_ReturnsCreatorEmail()
    {
        var workOrder = new WorkOrder
        {
            Creator = new Employee("jdoe", "Jane", "Doe", "jane@example.com")
        };

        workOrder.GetContactEmail().ShouldBe("jane@example.com");
    }

    [Test]
    public void GetContactEmail_WhenCreatorIsNull_ReturnsNull()
    {
        var workOrder = new WorkOrder { Creator = null };

        workOrder.GetContactEmail().ShouldBeNull();
    }
}
