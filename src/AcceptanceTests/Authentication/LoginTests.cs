using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Microsoft.Playwright;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class LoginTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public void VerifySetup()
    {
        var homer = TestHost.NewDbContext().Set<Employee>().Single(employee =>
            employee.UserName == "hsimpson");

        homer.ShouldNotBeNull();
    }

    [Test, Retry(2)]
    public async Task LoginWithUsernameOnlyForwardsToHomePage()
    {
        // Act: Go to home page
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1);

        var logoutLink = Page.GetByTestId(nameof(Logout.Elements.LogoutLink));
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.ClickAsync();
            await Page.WaitForURLAsync("**/");
        }

        // Click Login link in top bar
        await Click(nameof(LoginLink.Elements.LoginLink));
        await Page.WaitForURLAsync("**/login");
        await TakeScreenshotAsync(2);

        var memberSelect = Page.GetByTestId(nameof(Login.Elements.User));
        var homerOption = memberSelect.Locator("option[value='hsimpson']");
        await Expect(homerOption).ToHaveTextAsync("Homer Simpson");

        // Fill in username only
        await Select(nameof(Login.Elements.User), "hsimpson");
        await TakeScreenshotAsync(3);

        // Submit form
        await Click(nameof(UI.Shared.Pages.Login.Elements.LoginButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(4);

        // Assert: Should be redirected to home and see welcome message
        var welcomeTextLocator = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeTextLocator).ToHaveTextAsync("Welcome hsimpson!");
    }
}