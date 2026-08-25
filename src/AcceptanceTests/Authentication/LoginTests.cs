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
    public async Task Should_ExposeUserSelectIdMatchingLabelFor()
    {
        await Page.GotoAsync("/login");

        var userSelect = Page.GetByTestId(nameof(Login.Elements.User));
        var homerOption = userSelect.Locator("option[value='hsimpson']");
        await WaitForEmployeeOptionsRenderedAsync(homerOption);

        await Expect(userSelect).ToHaveAttributeAsync("id", nameof(Login.Elements.User));
        await Expect(Page.Locator($"label[for='{Login.Elements.User}']")).ToBeAttachedAsync();
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

    [Test, Retry(2)]
    public async Task Should_ShowLovejoyShortcut_OnLogin_WithoutSelectingMember()
    {
        await Page.GotoAsync("/login");

        var shortcut = Page.GetByTestId(nameof(Login.Elements.LovejoyShortcut));
        await shortcut.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000
        });

        await Expect(shortcut).ToBeVisibleAsync();
        await Expect(shortcut).ToHaveTextAsync("Log in as Timothy Lovejoy");
    }

    [Test, Retry(2)]
    public async Task Should_LoginAsTlovejoy_WhenLovejoyShortcutClicked()
    {
        await Page.GotoAsync("/login");

        var shortcut = Page.GetByTestId(nameof(Login.Elements.LovejoyShortcut));
        await shortcut.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000
        });

        await Click(nameof(Login.Elements.LovejoyShortcut));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var welcomeTextLocator = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeTextLocator).ToHaveTextAsync("Welcome tlovejoy!");
    }

    /// <summary>
    /// Regression (#9086): Lovejoy shortcut must await the in-flight EmployeeGetAllQuery
    /// so an early click still authenticates as tlovejoy once employees load.
    /// </summary>
    [Test, Retry(2)]
    public async Task Should_LoginAsTlovejoy_WhenLovejoyClickedBeforeEmployeesLoaded()
    {
        const int employeeQueryDelayMs = 1_000;

        await Page.RouteAsync("**/*blazor-wasm-single-api*", async route =>
        {
            if (!string.Equals(route.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                await route.ContinueAsync();
                return;
            }

            var postData = route.Request.PostData ?? string.Empty;
            if (postData.Contains("EmployeeGetAllQuery", StringComparison.Ordinal))
            {
                await Task.Delay(employeeQueryDelayMs);
            }

            await route.ContinueAsync();
        });

        await Page.GotoAsync("/login");
        var shortcut = Page.GetByTestId(nameof(Login.Elements.LovejoyShortcut));
        await shortcut.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 90_000
        });

        var employeeOptions = Page.GetByTestId(nameof(Login.Elements.User))
            .Locator("option[value]:not([value=''])");
        await Expect(employeeOptions).ToHaveCountAsync(0);

        await Click(nameof(Login.Elements.LovejoyShortcut));

        var welcomeTextLocator = Page.GetByTestId(nameof(Logout.Elements.WelcomeText));
        await Expect(welcomeTextLocator).ToHaveTextAsync(
            "Welcome tlovejoy!",
            new LocatorAssertionsToHaveTextOptions { Timeout = 60_000 });
    }
}
