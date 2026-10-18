using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class IndexFirstVisitTipTests
{
    [Test]
    public void IndexRazor_ShouldContainFirstVisitTip_ForNewVolunteers()
    {
        var razorPath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..",
            "UI.Shared", "Pages", "Index.razor"));

        File.ReadAllText(razorPath).ShouldContain("data-testid=\"first-visit-tip\"");
    }
}
