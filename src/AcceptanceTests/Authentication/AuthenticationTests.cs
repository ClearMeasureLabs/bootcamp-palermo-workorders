using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class AuthenticationTests : AcceptanceTestBase
{
    [Test]
    public async Task LoginPage_AllButtons_DisplayGreenStyling()
    {
        // Arrange: Navigate to login page
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var logoutLink = Page.GetByTestId(nameof(Logout.Elements.LogoutLink));
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.ClickAsync();
            await Page.WaitForURLAsync("**/");
        }

        await Click(nameof(LoginLink.Elements.LoginLink));
        await Page.WaitForURLAsync("**/login");
        await TakeScreenshotAsync(1, "LoginPage");

        // Act: Get the Login button
        var loginButton = Page.GetByTestId(nameof(UI.Shared.Pages.Login.Elements.LoginButton));
        await Expect(loginButton).ToBeVisibleAsync();

        // Assert: Verify Login button has green background color
        var buttonBackgroundColor = await loginButton.EvaluateAsync<string>(@"
            element => window.getComputedStyle(element).background
        ");
        
        buttonBackgroundColor.ShouldContain("22c55e", Case.Insensitive);
    }
}
