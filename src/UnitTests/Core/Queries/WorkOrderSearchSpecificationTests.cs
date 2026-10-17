using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

public class WorkOrderSearchSpecificationTests
{
    [Test]
    public void MatchTitleContains_WithText_SetsTitleContains()
    {
        var specification = new WorkOrderSearchSpecification();

        specification.MatchTitleContains("boiler");

        specification.TitleContains.ShouldBe("boiler");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void MatchTitleContains_WithBlankText_ClearsFilter(string? text)
    {
        var specification = new WorkOrderSearchSpecification();
        specification.MatchTitleContains("boiler");

        specification.MatchTitleContains(text);

        specification.TitleContains.ShouldBeNull();
    }
}
