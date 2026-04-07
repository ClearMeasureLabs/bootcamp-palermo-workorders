using System.Globalization;
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
        await EnsureLoggedOutAsync();

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        var animationName = await loginLink.EvaluateAsync<string>(
            "el => getComputedStyle(el).animationName");
        animationName.ShouldNotBe("none");

        var durationText = await loginLink.EvaluateAsync<string>(
            "el => getComputedStyle(el).animationDuration");
        ParseCssSeconds(durationText).ShouldBeGreaterThanOrEqualTo(1.5);

        var minOpacity = 1.0;
        var maxOpacity = 0.0;
        for (var i = 0; i < 45; i++)
        {
            var opacity = await loginLink.EvaluateAsync<double>(
                "el => parseFloat(getComputedStyle(el).opacity)");
            if (opacity < minOpacity)
            {
                minOpacity = opacity;
            }

            if (opacity > maxOpacity)
            {
                maxOpacity = opacity;
            }

            await Page.WaitForTimeoutAsync(100);
        }

        (maxOpacity - minOpacity).ShouldBeGreaterThan(0.2);
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

    private static double ParseCssSeconds(string value)
    {
        var token = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
        if (token.EndsWith("ms", StringComparison.Ordinal))
        {
            return double.Parse(token[..^2], CultureInfo.InvariantCulture) / 1000.0;
        }

        if (token.EndsWith('s'))
        {
            return double.Parse(token[..^1], CultureInfo.InvariantCulture);
        }

        return double.Parse(token, CultureInfo.InvariantCulture);
    }
}
