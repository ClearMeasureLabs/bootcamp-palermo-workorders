namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class LandingPageTests : AcceptanceTestBase
{
    protected override bool LoadDataOnSetup => false;

    [Test]
    public async Task Should_DisplayNewQuote_WhenVisitingLandingPage()
    {
        // Arrange
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Act
        var quoteText = await Page.Locator(".church-footer p").InnerTextAsync();

        // Assert
        quoteText.ShouldBe("\"Et omnis qui audit verba mea haec et non facit ea similis erit viro stulto qui aedificavit domum suam supra harenam.\" - Matthew 7:26");
    }
}
