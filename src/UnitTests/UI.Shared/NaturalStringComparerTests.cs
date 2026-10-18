using ClearMeasure.Bootcamp.UI.Shared.Services;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

public class NaturalStringComparerTests
{
    [Test]
    public void Compare_RoomLabelsWithNumbers_OrdersByNumericValue()
    {
        string[] items = ["Room 10", "Room 12", "Room 9"];

        var sorted = items.OrderBy(r => r, NaturalStringComparer.OrdinalIgnoreCase).ToArray();

        sorted.ShouldBe(["Room 9", "Room 10", "Room 12"]);
    }

    [Test]
    public void Compare_Descending_OrdersByNumericValueDescending()
    {
        string[] items = ["Room 10", "Room 12", "Room 9"];

        var sorted = items.OrderByDescending(r => r, NaturalStringComparer.OrdinalIgnoreCase).ToArray();

        sorted.ShouldBe(["Room 12", "Room 10", "Room 9"]);
    }
}
