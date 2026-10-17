using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkOrderSearchSpecificationTests
{
    [Test]
    public void MatchTitleContains_WithText_SetsTitleContains()
    {
        var spec = new WorkOrderSearchSpecification();
        spec.MatchTitleContains("plumbing");
        spec.TitleContains.ShouldBe("plumbing");
    }

    [Test]
    public void MatchTitleContains_WithBlank_SetsNull()
    {
        var spec = new WorkOrderSearchSpecification();
        spec.MatchTitleContains("  ");
        spec.TitleContains.ShouldBeNull();
    }

    [Test]
    public void MatchTitleContains_WithNull_SetsNull()
    {
        var spec = new WorkOrderSearchSpecification();
        spec.MatchTitleContains(null);
        spec.TitleContains.ShouldBeNull();
    }
}
