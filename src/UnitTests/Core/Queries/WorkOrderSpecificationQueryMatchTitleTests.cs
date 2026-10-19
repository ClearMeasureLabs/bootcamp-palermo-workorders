using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

[TestFixture]
public class WorkOrderSpecificationQueryMatchTitleTests
{
    [Test]
    public void MatchTitle_ShouldSetTitleContains_WhenTextProvided()
    {
        var query = new WorkOrderSpecificationQuery();

        query.MatchTitle("kitchen");

        query.TitleContains.ShouldBe("kitchen");
    }

    [Test]
    public void MatchTitle_ShouldClearTitleContains_WhenNullOrWhitespace()
    {
        var query = new WorkOrderSpecificationQuery();
        query.MatchTitle("kitchen");

        query.MatchTitle("  ");
        query.TitleContains.ShouldBeNull();

        query.MatchTitle(null);
        query.TitleContains.ShouldBeNull();
    }
}
