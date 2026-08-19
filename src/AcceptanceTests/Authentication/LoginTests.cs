using System.Globalization;
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

    [Test, Retry(2)]
    public async Task LoginLink_ShouldBlinkOnUnauthenticatedPage_UntilLogin()
    {
        var logoutLink = Page.GetByTestId(nameof(Logout.Elements.LogoutLink));
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.ClickAsync();
            await Page.WaitForURLAsync("**/");
        }

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        var animationName = await loginLink.EvaluateAsync<string>(
            "el => window.getComputedStyle(el).animationName");
        animationName.ShouldNotBe("none");
        animationName.ShouldContain("login-blink");

        await Click(nameof(LoginLink.Elements.LoginLink));
        await Page.WaitForURLAsync("**/login");
        await Select(nameof(Login.Elements.User), "hsimpson");
        await Click(nameof(Login.Elements.LoginButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(LoginLink.Elements.LoginLink))).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId(nameof(Logout.Elements.WelcomeText))).ToHaveTextAsync("Welcome hsimpson!");
    }

    [Test, Retry(2)]
    public async Task LoginLink_ShouldBlinkAcrossMultipleUnauthenticatedPages()
    {
        var logoutLink = Page.GetByTestId(nameof(Logout.Elements.LogoutLink));
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.ClickAsync();
            await Page.WaitForURLAsync("**/");
        }

        foreach (var path in new[] { "/", "/counter" })
        {
            await Page.GotoAsync(path);
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
            await Expect(loginLink).ToBeVisibleAsync();

            var animationName = await loginLink.EvaluateAsync<string>(
                "el => window.getComputedStyle(el).animationName");
            animationName.ShouldNotBe("none");
            animationName.ShouldContain("login-blink");
        }

        await Click(nameof(LoginLink.Elements.LoginLink));
        await Page.WaitForURLAsync("**/login");
        await Select(nameof(Login.Elements.User), "hsimpson");
        await Click(nameof(Login.Elements.LoginButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(LoginLink.Elements.LoginLink))).ToHaveCountAsync(0);
    }

    [Test, Retry(2)]
    public async Task LoginLink_BlinkAnimation_ShouldRespectReducedMotionPreference()
    {
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        var logoutLink = Page.GetByTestId(nameof(Logout.Elements.LogoutLink));
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.ClickAsync();
            await Page.WaitForURLAsync("**/");
        }

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        var styles = await loginLink.EvaluateAsync<Dictionary<string, string>>(@"el => {
            const cs = window.getComputedStyle(el);
            return {
                animationName: cs.animationName,
                fontWeight: cs.fontWeight,
                borderWidth: cs.borderTopWidth
            };
        }");

        styles["animationName"].ShouldBe("none");
        int.Parse(styles["fontWeight"], CultureInfo.InvariantCulture).ShouldBeGreaterThanOrEqualTo(700);
        styles["borderWidth"].ShouldNotBe("0px");
    }
}