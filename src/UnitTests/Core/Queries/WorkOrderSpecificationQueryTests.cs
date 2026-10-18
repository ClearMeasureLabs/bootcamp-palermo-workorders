using ClearMeasure.Bootcamp.Core.Queries;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

[TestFixture]
public class WorkOrderSpecificationQueryTests
{
    [Test]
    public void MatchDueBefore_ShouldSetDueBefore()
    {
        var query = new WorkOrderSpecificationQuery();
        var dueBefore = new DateOnly(2026, 2, 1);

        query.MatchDueBefore(dueBefore);

        query.DueBefore.ShouldBe(dueBefore);
    }

    [Test]
    public void ShouldRemotableRequest_RoundTrip_DueBefore()
    {
        var query = new WorkOrderSpecificationQuery();
        query.MatchDueBefore(new DateOnly(2026, 2, 1));

        var rehydrated = (WorkOrderSpecificationQuery)RemotableRequestTests.AssertRemotable(query);

        rehydrated.DueBefore.ShouldBe(new DateOnly(2026, 2, 1));
    }
}
