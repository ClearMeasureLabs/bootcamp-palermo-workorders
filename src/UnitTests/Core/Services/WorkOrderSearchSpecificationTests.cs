using ClearMeasure.Bootcamp.Core.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Services;

[TestFixture]
public class WorkOrderSearchSpecificationTests
{
    [Test]
    public void MatchRoomNumber_WhenValueProvided_SetsRoomNumber()
    {
        var specification = new WorkOrderSearchSpecification();

        specification.MatchRoomNumber("101");

        specification.RoomNumber.ShouldBe("101");
    }

    [Test]
    public void MatchRoomNumber_WhenNull_SetsRoomNumberToNull()
    {
        var specification = new WorkOrderSearchSpecification();
        specification.MatchRoomNumber("101");

        specification.MatchRoomNumber(null);

        specification.RoomNumber.ShouldBeNull();
    }
}
