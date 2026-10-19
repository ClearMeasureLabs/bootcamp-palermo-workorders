using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class WorkOrderSearchProjectorTests
{
    [Test]
    public void ShouldUseLargeBaseFontOnSearchPageForProjector()
    {
        var css = ReadScopedCss("WorkOrderSearch.razor.css");

        css.ShouldContain(".search-page");
        css.ShouldContain("font-size: 1.25rem");
        css.ShouldContain("font-size: 3rem");
        css.ShouldContain("font-size: 1.35rem");
    }

    private static string ReadScopedCss(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..",
            "UI.Shared", "Pages", fileName));
        return File.ReadAllText(path);
    }
}
