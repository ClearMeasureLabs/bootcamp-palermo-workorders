using ClearMeasure.Bootcamp.UI.Shared;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared;

[TestFixture]
public class LoginDisplayNameFormatterTests
{
    [Test]
    public void FormatForLoginDropdown_MixedCase_ReturnsLastNameInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("mary jane SIMPSON").ShouldBe("SIMPSON, M.");
    }

    [Test]
    public void FormatForLoginDropdown_AlreadyUppercase_ReturnsLastNameInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("HOMER SIMPSON").ShouldBe("SIMPSON, H.");
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
    public void FormatForLoginDropdown_WhitespaceOnly_ReturnsEmpty()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("  \t ").ShouldBe(string.Empty);
    }

    [Test]
    public void FormatForLoginDropdown_HyphenAndApostrophe_ReturnsLastNameInitial()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("Mary-Jane O'Brien")
            .ShouldBe("O'BRIEN, M.");
    }

    [Test]
    public void FormatForLoginDropdown_LastNameOnly_ReturnsLastName()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown("SIMPSON").ShouldBe("SIMPSON");
    }

    [Test]
    public void FormatForLoginDropdown_EmptyFirstName_ReturnsLastName()
    {
        LoginDisplayNameFormatter.FormatForLoginDropdown(" FLANDERS").ShouldBe("FLANDERS");
    }
}
