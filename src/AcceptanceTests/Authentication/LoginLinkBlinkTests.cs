using ClearMeasure.Bootcamp.UI.Shared.Components;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class LoginLinkBlinkTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Should_ApplyLoginLinkPulseAnimation_WhenNotAuthenticated()
    {
        await EnsureLoggedOutAsync();

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        var animationName = await loginLink.EvaluateAsync<string>(
            "el => getComputedStyle(el).animationName");
        animationName.ShouldNotBe("none");
        animationName.ShouldContain("loginLinkPulse");

        var animationDuration = await loginLink.EvaluateAsync<string>(
            "el => getComputedStyle(el).animationDuration");
        animationDuration.ShouldContain("1s");
    }

    [Test, Retry(2)]
    public async Task Should_StopLoginLinkPulse_WhenAuthenticated()
    {
        await LoginAsHomerSimpsonAsync();

        await Expect(Page.GetByTestId(nameof(LoginLink.Elements.LoginLink))).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId(nameof(Logout.Elements.LogoutLink))).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task Should_KeepLoginLinkVisibleAndClickable_WhilePulsing()
    {
        await EnsureLoggedOutAsync();

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();
        await Click(nameof(LoginLink.Elements.LoginLink));
        await Page.WaitForURLAsync("**/login");
        await Expect(Page.GetByTestId(nameof(Login.Elements.User))).ToBeVisibleAsync();
    }

    [Test, Retry(2)]
    public async Task Should_UseStaticAccent_WhenPrefersReducedMotion()
    {
        await EnsureLoggedOutAsync();
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        var animationName = await loginLink.EvaluateAsync<string>(
            "el => getComputedStyle(el).animationName");
        animationName.ShouldBe("none");

        var borderWidth = await loginLink.EvaluateAsync<string>(
            "el => getComputedStyle(el).borderTopWidth");
        borderWidth.ShouldNotBe("0px");
    }

    [Test, Retry(2)]
    public async Task Should_ApplyDarkThemePulse_WhenDataThemeDark()
    {
        await EnsureLoggedOutAsync();
        await Page.EvaluateAsync("() => document.documentElement.setAttribute('data-theme', 'dark')");

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        var animationName = await loginLink.EvaluateAsync<string>(
            "el => getComputedStyle(el).animationName");
        animationName.ShouldNotBe("none");
        animationName.ShouldContain("loginLinkPulse");
    }

    private async Task EnsureLoggedOutAsync()
    {
        var logoutLink = Page.GetByTestId(nameof(Logout.Elements.LogoutLink));
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.ClickAsync();
            await Page.WaitForURLAsync("**/");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    private async Task LoginAsHomerSimpsonAsync()
    {
        await EnsureLoggedOutAsync();
        await Click(nameof(LoginLink.Elements.LoginLink));
        await Page.WaitForURLAsync("**/login");

        var userSelect = Page.GetByTestId(nameof(Login.Elements.User));
        var homerOption = userSelect.Locator("option[value='hsimpson']");
        await homerOption.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 90_000
        });

        await Select(nameof(Login.Elements.User), "hsimpson");
        await Click(nameof(Login.Elements.LoginButton));
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Expect(Page.GetByTestId(nameof(Logout.Elements.WelcomeText)))
            .ToHaveTextAsync("Welcome hsimpson!");
    }
}
