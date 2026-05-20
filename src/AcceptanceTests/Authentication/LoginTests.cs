using ClearMeasure.Bootcamp.UI.Shared.Components;
using LoginPage = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

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

        // Fill in username only
        await Select(nameof(LoginPage.Elements.User), "hsimpson");
        await TakeScreenshotAsync(3);

        // Submit form
        await Click(nameof(LoginPage.Elements.LoginButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(4);

        // Assert: Should be redirected to home and see welcome message
        var welcomeTextLocator = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeTextLocator).ToHaveTextAsync("Welcome hsimpson!");
    }

    [Test, Retry(2)]
    public async Task LoginDropdownOptionLabelsAreUppercase()
    {
        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var memberSelect = Page.GetByTestId(nameof(LoginPage.Elements.User));
        await Expect(memberSelect).ToBeVisibleAsync();

        var hsimpsonOption = memberSelect.Locator("option[value=\"hsimpson\"]");
        await Expect(hsimpsonOption).ToHaveTextAsync("HOMER SIMPSON");

        var jpalermoOption = memberSelect.Locator("option[value=\"jpalermo\"]");
        await Expect(jpalermoOption).ToHaveTextAsync("JEFFREY PALERMO");
    }
}