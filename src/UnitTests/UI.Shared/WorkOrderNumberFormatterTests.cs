using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

public class WorkOrderNumberFormatterTests
{
    [Test]
    public void Format_Null_ReturnsEmptyString()
    {
        WorkOrderNumberFormatter.Format(null).ShouldBe("");
    }

    [Test]
    public void Format_EmptyString_ReturnsEmptyString()
    {
        WorkOrderNumberFormatter.Format("").ShouldBe("");
    }

    [Test]
    public void Format_RawNumber_PrefixesWithWO()
    {
        WorkOrderNumberFormatter.Format("1").ShouldBe("WO-1");
    }

    [Test]
    public void Format_Zero_PrefixesWithWO()
    {
        WorkOrderNumberFormatter.Format("0").ShouldBe("WO-0");
    }

    [Test]
    public void Format_MultiDigitNumber_PrefixesWithWO()
    {
        WorkOrderNumberFormatter.Format("1001").ShouldBe("WO-1001");
    }
}
