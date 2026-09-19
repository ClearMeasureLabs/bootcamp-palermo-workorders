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
    public void Format_EmString_ReturnsEmDash()
    {
        RoomNumberDisplayFormatter.Format(string.Empty).ShouldBe("\u2014");
    }

    [Test]
    public void Format_WhitespaceOnly_ReturnsEmDash()
    {
        RoomNumberDisplayFormatter.Format("  ").ShouldBe("\u2014");
    }

    [Test]
    public void Format_NormalValue_ReturnsTrimmedValue()
    {
        RoomNumberDisplayFormatter.Format("R101").ShouldBe("R101");
    }

    [Test]
    public void Format_ValueWithSurroundingWhitespace_ReturnsTrimmedValue()
    {
        RoomNumberDisplayFormatter.Format(" R200 ").ShouldBe("R200");
    }
}
