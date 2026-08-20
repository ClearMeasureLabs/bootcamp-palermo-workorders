using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Api;

[TestFixture]
public class ErrorPageStaticAssetsTests
{
    [Test]
    public void ErrorCshtml_ShouldNotReferenceMissingSharedCssPaths()
    {
        var errorPage = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "UI", "Api", "Pages", "Error.cshtml"));

        File.Exists(errorPage).ShouldBeTrue();
        var markup = File.ReadAllText(errorPage);
        markup.ShouldNotContain("href=\"~/css/");
        markup.ShouldContain("<style>");
        markup.ShouldContain("text-danger");
    }
}
