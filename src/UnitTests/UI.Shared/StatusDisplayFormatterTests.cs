using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class StatusDisplayFormatterTests
{
    [Test]
    public void Format_NonEmptyString_ReturnsInput()
    {
        StatusDisplayFormatter.Format("Draft").ShouldBe("Draft");
    }

    [Test]
    public void Format_MultiWordString_ReturnsInput()
    {
        StatusDisplayFormatter.Format("In Progress").ShouldBe("In Progress");
    }

    [Test]
    public void Format_Null_ReturnsEmpty()
    {
        StatusDisplayFormatter.Format(null).ShouldBe(string.Empty);
    }

    [Test]
    public void Format_WhitespaceOnly_ReturnsEmpty()
    {
        StatusDisplayFormatter.Format("   ").ShouldBe(string.Empty);
    }
}
