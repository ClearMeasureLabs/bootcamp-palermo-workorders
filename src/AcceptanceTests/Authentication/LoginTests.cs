using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class LoginTests : AcceptanceTestBase
{
    /// <summary>
    /// The employee &lt;option&gt; elements are rendered only after the Blazor WASM app
    /// boots interactively and the employee list loads. WaitForLoadStateAsync(NetworkIdle)
    /// does not guarantee that render has happened, so wait for the option itself.
    /// Options live inside a closed &lt;select&gt; and therefore have no bounding box —
    /// they are never "visible" to Playwright, so wait for Attached, not Visible.
    /// </summary>
    private static Task WaitForEmployeeOptionsRenderedAsync(ILocator employeeOption) =>
        employeeOption.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 90_000
        });

    [Test, Retry(2)]
    public void VerifySetup()
    {
        var homer = TestHost.NewDbContext().Set<Employee>().Single(employee =>
            employee.UserName == "hsimpson");

        homer.ShouldNotBeNull();
    }

    [Test, Retry(2)]
    public async Task Should_DisplayUppercaseNames_InLoginDropdown()
    {
        await Page.GotoAsync("/login");

        var userSelect = Page.GetByTestId(nameof(Login.Elements.User));
        var homerOption = userSelect.Locator("option[value='hsimpson']");
        await WaitForEmployeeOptionsRenderedAsync(homerOption);

        var placeholderOption = userSelect.Locator("option[value='']");
        await Expect(placeholderOption).ToHaveTextAsync("-- Select a parishioner or staff member --");
        await Expect(homerOption).ToHaveTextAsync("HOMER SIMPSON");
    }

    [Test, Retry(2)]
    public async Task Should_LoginSuccessfully_UsingUsernameValue_NotDisplayLabel()
    {
        await Page.GotoAsync("/login");

        var userSelect = Page.GetByTestId(nameof(Login.Elements.User));
        var homerOption = userSelect.Locator("option[value='hsimpson']");
        await WaitForEmployeeOptionsRenderedAsync(homerOption);
        await Expect(homerOption).ToHaveTextAsync("HOMER SIMPSON");

        await Select(nameof(Login.Elements.User), "hsimpson");
        await Click(nameof(Login.Elements.LoginButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var welcomeTextLocator = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeTextLocator).ToHaveTextAsync("Welcome hsimpson!");
    }

    [Test, Retry(2)]
    public async Task LoginWithUsernameOnlyForwardsToHomePage()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(1);

        var logoutLink = Page.GetByTestId(nameof(Logout.Elements.LogoutLink));
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.ClickAsync();
            await Page.WaitForURLAsync("**/");
        }

        await Click(nameof(LoginLink.Elements.LoginLink));
        await Page.WaitForURLAsync("**/login");
        await TakeScreenshotAsync(2);

        await Select(nameof(Login.Elements.User), "hsimpson");
        await TakeScreenshotAsync(3);

        await Click(nameof(Login.Elements.LoginButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await TakeScreenshotAsync(4);

        var welcomeTextLocator = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeTextLocator).ToHaveTextAsync("Welcome hsimpson!");
    }
}