using Bunit;
using Shouldly;
using IndexPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Index;
using TestContext = Bunit.TestContext;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Pages;

[TestFixture]
public class IndexPageTests
{
    [Test]
    public void ShouldDisplayGreetingBanner()
    {
        using var ctx = new TestContext();

        var component = ctx.RenderComponent<IndexPage>();

        var banner = component.Find($"[data-testid='{nameof(IndexPage.Elements.GreetingBanner)}']");
        banner.ShouldNotBeNull();
        banner.TextContent.ShouldContain("Welcome to the AI Software Factory!");
    }
}
