using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class LoginTests : AcceptanceTestBase
{
    [Test]
    public void VerifySetup()
    {
        var homer = TestHost.NewDbContext().Set<Employee>().Single(employee =>
            employee.UserName == "hsimpson");

        homer.ShouldNotBeNull();
    }

    [Test]
    [Repeat(2)]
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
        await Select(nameof(UI.Shared.Pages.Login.Elements.User), "hsimpson");
        await TakeScreenshotAsync(3);

        // Submit form
        await Click(nameof(UI.Shared.Pages.Login.Elements.LoginButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(4);

        // Assert: Should be redirected to home and see welcome message
        var welcomeTextLocator = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeTextLocator).ToHaveTextAsync("Welcome hsimpson!");
    }

    [Test]
    public async Task LoginButton_IsGreen()
    {
        // Navigate to login page
        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1);

        // Verify login button has green color styling
        var loginButton = Page.GetByTestId(nameof(UI.Shared.Pages.Login.Elements.LoginButton));
        await Expect(loginButton).ToBeVisibleAsync();

        // Get computed background color
        var backgroundColor = await loginButton.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundColor");
        backgroundColor.ShouldContain("34, 197, 94"); // RGB for green (#22c55e)

        // Verify button remains green on hover state
        await loginButton.HoverAsync();
        await TakeScreenshotAsync(2);
        var hoverBackgroundColor = await loginButton.EvaluateAsync<string>("el => window.getComputedStyle(el).backgroundColor");
        hoverBackgroundColor.ShouldContain("22, 163, 74"); // RGB for darker green (#16a34a)
    }
}