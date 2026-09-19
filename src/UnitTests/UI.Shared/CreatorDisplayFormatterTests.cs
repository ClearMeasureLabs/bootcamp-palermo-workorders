using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class CreatorDisplayFormatterTests
{
    [Test]
    public void Format_SimpsonHomer_ReturnsUppercaseWithInitial()
    {
        CreatorDisplayFormatter.Format("Simpson", "Homer").ShouldBe("SIMPSON, H.");
    }

    [Test]
    public void Format_DoeJane_ReturnsUppercaseWithInitial()
    {
        CreatorDisplayFormatter.Format("Doe", "Jane").ShouldBe("DOE, J.");
    }

    [Test]
    public void Format_NullLastName_ReturnsEmpty()
    {
        CreatorDisplayFormatter.Format(null, null).ShouldBe(string.Empty);
    }

    [Test]
    public void Format_EmptyLastName_ReturnsEmpty()
    {
        CreatorDisplayFormatter.Format(string.Empty, string.Empty).ShouldBe(string.Empty);
    }

    [Test]
    public void Format_NullLastName_WithFirstName_ReturnsEmpty()
    {
        CreatorDisplayFormatter.Format(null, "Homer").ShouldBe(string.Empty);
    }

    [Test]
    public void Format_LastName_WithNullFirstName_ReturnsUppercaseLastName()
    {
        CreatorDisplayFormatter.Format("Simpson", null).ShouldBe("SIMPSON");
    }

    [Test]
    public void Format_LastName_WithEmptyFirstName_ReturnsUppercaseLastName()
    {
        CreatorDisplayFormatter.Format("Simpson", string.Empty).ShouldBe("SIMPSON");
    }

    [Test]
    public void Format_AllCapsNames_ReturnsUppercaseWithInitial()
    {
        CreatorDisplayFormatter.Format("SIMPSON", "HOMER").ShouldBe("SIMPSON, H.");
    }

    [Test]
    public void Format_MixedCaseNames_ReturnsUppercaseWithInitial()
    {
        CreatorDisplayFormatter.Format("O'Brien", "Mary-Jane").ShouldBe("O'BRIEN, M.");
    }

    [Test]
    public void Format_SingleCharFirstName_ReturnsUppercaseWithInitial()
    {
        CreatorDisplayFormatter.Format("Smith", "A").ShouldBe("SMITH, A.");
    }
}
