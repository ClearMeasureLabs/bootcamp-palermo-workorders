using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class RoomNumberDisplayFormatterTests
{
    [Test]
    public void Format_Null_ReturnsEmDash()
    {
        RoomNumberDisplayFormatter.Format(null).ShouldBe("\u2014");
    }

    [Test]
    public void Format_EmptyString_ReturnsEmDash()
    {
        RoomNumberDisplayFormatter.Format(string.Empty).ShouldBe("\u2014");
    }

    [Test]
    public void Format_WhitespaceOnly_ReturnsEmDash()
    {
        RoomNumberDisplayFormatter.Format("   ").ShouldBe("\u2014");
    }

    [Test]
    public void Format_ValidRoomNumber_ReturnsTrimmed()
    {
        RoomNumberDisplayFormatter.Format("204").ShouldBe("204");
    }
}
