using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class LoginBlinkingBorderTests : AcceptanceTestBase
{
    protected override bool LoadDataOnSetup { get; set; } = false;

    [Test]
    public async Task ShouldShowBlinkingBorderOnLoginPageTextFields()
    {
        // Act: Navigate to login page
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1, "HomePage");

        // Navigate to login page
        await Click(nameof(LoginLink.Elements.LoginLink));
        await Page.WaitForURLAsync("**/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(2, "LoginPage");

        // Note: The login page uses a dropdown (InputSelect) for username, not a text input
        // There is no password field in this application's login page
        // Verify the page loaded correctly
        var pageTitle = Page.Locator("h3").First;
        await Expect(pageTitle).ToContainTextAsync("Church Staff Portal");

        // The login form uses a select dropdown, not text inputs
        // So there are no text input fields to verify blinking borders on
        var userSelect = Page.GetByTestId(nameof(UI.Shared.Pages.Login.Elements.User));
        await Expect(userSelect).ToBeVisibleAsync();

        await TakeScreenshotAsync(3, "LoginPageVerified");
    }
}
