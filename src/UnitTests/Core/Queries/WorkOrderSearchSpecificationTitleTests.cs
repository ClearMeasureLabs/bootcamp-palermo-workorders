using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

[TestFixture]
public class WorkOrderSearchSpecificationTitleTests
{
    [Test]
    public void MatchTitleContains_WithText_SetsTitleContains()
    {
        var specification = new WorkOrderSearchSpecification();
        specification.MatchTitleContains("plumbing");
        specification.TitleContains.ShouldBe("plumbing");
    }

    [Test]
    public void MatchTitleContains_WithBlank_KeepsBlankValue()
    {
        var specification = new WorkOrderSearchSpecification();
        specification.MatchTitleContains("   ");
        specification.TitleContains.ShouldBe("   ");
    }
}
