using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client;

[TestFixture]
public class IndexHtmlThemeScriptTests
{
    private string _markup = string.Empty;

    [SetUp]
    public void SetUp()
    {
        var indexHtml = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI", "Client", "wwwroot", "index.html"));

        File.Exists(indexHtml).ShouldBeTrue();
        _markup = File.ReadAllText(indexHtml);
    }

    [Test]
    public void IndexHtml_ThemeScript_ShouldNotContainPreferColorSchemeBranch()
    {
        _markup.ShouldNotContain("prefers-color-scheme");
    }

    [Test]
    public void IndexHtml_ThemeScript_ShouldDefaultToLightWhenNoStoredPreference()
    {
        _markup.ShouldContain("? v : 'light'");
    }
}
