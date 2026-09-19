using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class DueDateEmptyFormatterTests
{
    [Test]
    public void Format_Null_ReturnsEmDash()
    {
        DueDateEmptyFormatter.Format(null).ShouldBe("\u2014");
    }

    [Test]
    public void Format_Empty_ReturnsEmDash()
    {
        DueDateEmptyFormatter.Format(string.Empty).ShouldBe("\u2014");
    }

    [Test]
    public void Format_Whitespace_ReturnsEmDash()
    {
        DueDateEmptyFormatter.Format("  \t ").ShouldBe("\u2014");
    }

    [Test]
    public void Format_ValidDate_Unchanged()
    {
        DueDateEmptyFormatter.Format("Sep 12, 2026").ShouldBe("Sep 12, 2026");
    }
}
