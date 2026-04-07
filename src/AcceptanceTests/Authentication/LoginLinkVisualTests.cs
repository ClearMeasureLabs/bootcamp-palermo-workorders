using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.Playwright;
using Shouldly;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class LoginLinkVisualTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task LoginLink_Visible_And_Emphasized_WhenUnauthenticated()
    {
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await EnsureLoggedOutAsync();

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        var animationName = await loginLink.EvaluateAsync<string>(
            "el => getComputedStyle(el).animationName");
        animationName.ShouldNotBe("none");
    }

    [Test, Retry(2)]
    public async Task LoginLink_OpacityDipsBelowThreshold_WhenMotionAllowed()
    {
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.NoPreference });
        await EnsureLoggedOutAsync();

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        const double dimThreshold = 0.55;
        var sawDim = false;
        for (var i = 0; i < 30; i++)
        {
            var opacity = await loginLink.EvaluateAsync<double>(
                "el => parseFloat(getComputedStyle(el).opacity)");
            if (opacity < dimThreshold)
            {
                sawDim = true;
                break;
            }

            await Task.Delay(80);
        }

        sawDim.ShouldBeTrue("login link opacity should dip during login-prompt-emphasis when motion is allowed");
    }

    [Test, Retry(2)]
    public async Task LoginLink_StaticEmphasis_WhenPrefersReducedMotion_Reduce()
    {
        await Page.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        await EnsureLoggedOutAsync();

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        var animationName = await loginLink.EvaluateAsync<string>(
            "el => getComputedStyle(el).animationName");
        animationName.ShouldBe("none");

        var boxShadow = await loginLink.EvaluateAsync<string>(
            "el => getComputedStyle(el).boxShadow");
        boxShadow.ShouldNotBe("none");
    }

    [Test, Retry(2)]
    public async Task LoginLink_FocusVisible_OnKeyboardFocus()
    {
        await EnsureLoggedOutAsync();

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        for (var i = 0; i < 30; i++)
        {
            await Page.Keyboard.PressAsync("Tab");
            var testId = await Page.EvaluateAsync<string?>(
                "() => document.activeElement?.getAttribute('data-testid')");
            if (testId == nameof(LoginLink.Elements.LoginLink))
            {
                break;
            }
        }

        var outlineWidth = await loginLink.EvaluateAsync<string>(
            "el => getComputedStyle(el).outlineWidth");
        outlineWidth.ShouldNotBe("0px");
    }

    [Test, Retry(2)]
    public async Task LoginLink_LayoutOk_OnNarrowViewport()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await EnsureLoggedOutAsync();

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        var title = Page.Locator(".header-title h3").First;
        await Expect(title).ToBeVisibleAsync();

        var loginBox = await loginLink.BoundingBoxAsync();
        var titleBox = await title.BoundingBoxAsync();
        loginBox.ShouldNotBeNull();
        titleBox.ShouldNotBeNull();

        loginBox!.Y.ShouldBeGreaterThanOrEqualTo(titleBox!.Y + titleBox.Height - 2);
    }

    private async Task EnsureLoggedOutAsync()
    {
        await Page.GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var logoutLink = Page.GetByTestId(nameof(Logout.Elements.LogoutLink));
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.ClickAsync();
            await Page.WaitForURLAsync("**/");
        }
    }
}
