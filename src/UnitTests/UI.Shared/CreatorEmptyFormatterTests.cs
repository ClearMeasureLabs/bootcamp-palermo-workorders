using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class CreatorEmptyFormatterTests
{
    [Test]
    public void Format_Null_ReturnsEmDash()
    {
        CreatorEmptyFormatter.Format(null).ShouldBe("\u2014");
    }

    [Test]
    public void Format_Empty_ReturnsEmDash()
    {
        CreatorEmptyFormatter.Format(string.Empty).ShouldBe("\u2014");
    }

    [Test]
    public void Format_WhitespaceOnly_ReturnsEmDash()
    {
        CreatorEmptyFormatter.Format("   ").ShouldBe("\u2014");
    }

    [Test]
    public void Format_ValidName_ReturnsUnchanged()
    {
        CreatorEmptyFormatter.Format("Homer Simpson").ShouldBe("Homer Simpson");
    }
}
