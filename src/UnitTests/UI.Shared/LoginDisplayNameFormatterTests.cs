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

    [Test]
    public void FormatForLoginDropdown_Null_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(null).ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_Empty_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(string.Empty).ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_WhitespaceOnly_PreservesLengthAndCaseMapsToUpperInvariant()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("  \t ").ShouldBe("  \t ".ToUpperInvariant());
    }
}
