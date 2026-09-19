using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class ResultsCountFormatterTests
{
    [Test]
    public void Format_Zero_ReturnsZeroCount()
    {
        ResultsCountFormatter.Format(0).ShouldBe("0 work orders found");
    }

    [Test]
    public void Format_One_ReturnsOneCount()
    {
        ResultsCountFormatter.Format(1).ShouldBe("1 work orders found");
    }

    [Test]
    public void Format_Three_ReturnsThreeCount()
    {
        ResultsCountFormatter.Format(3).ShouldBe("3 work orders found");
    }
}
