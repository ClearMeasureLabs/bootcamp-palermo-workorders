using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class LoginDisplayNameFormatterTests
{
    [Test]
    public void FormatForLoginDropdown_MixedCase_ReturnsUppercase()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("mary jane SIMPSON").ShouldBe("MARY JANE SIMPSON");
    }

    [Test]
    public void FormatForLoginDropdown_AlreadyUppercase_Unchanged()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("HOMER SIMPSON").ShouldBe("HOMER SIMPSON");
    }
}
