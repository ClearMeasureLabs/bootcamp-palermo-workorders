using ClearMeasure.Bootcamp.UI.Shared.Components;
using Microsoft.Playwright;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class LoginLinkAcceptanceTests : AcceptanceTestBase
{
    private static async Task EnsureLoggedOutAsync(IPage page)
    {
        var logoutLink = page.GetByTestId(nameof(Logout.Elements.LogoutLink));
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.ClickAsync();
            await page.WaitForURLAsync("**/login");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await page.GotoAsync("/");
            await page.WaitForURLAsync("/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
    }

    [Test, Retry(2)]
    public async Task LoggedOutHeader_LoginLink_HasActiveAnimationWhenMotionAllowed()
    {
        await EnsureLoggedOutAsync(Page);

        var link = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(link).ToBeVisibleAsync();

        var animationName = await link.EvaluateAsync<string>("el => getComputedStyle(el).animationName");
        animationName.ShouldNotBe("none");

        var opacity1 = await link.EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).opacity)");
        var shadow1 = await link.EvaluateAsync<string>("el => getComputedStyle(el).boxShadow");
        await Task.Delay(1200);
        var opacity2 = await link.EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).opacity)");
        var shadow2 = await link.EvaluateAsync<string>("el => getComputedStyle(el).boxShadow");
        var opacityChanged = Math.Abs(opacity1 - opacity2) > 0.05;
        var shadowChanged = shadow1 != shadow2;
        (opacityChanged || shadowChanged).ShouldBeTrue();
    }

    [Test, Retry(2)]
    public async Task LoggedOutHeader_LoginLink_ReducedMotion_HasNoAnimationAndStaticEmphasis()
    {
        await RunWithExtraBrowserContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = ServerFixture.ApplicationBaseUrl,
                IgnoreHTTPSErrors = true,
                ViewportSize = new ViewportSize { Width = 800, Height = 600 },
                ReducedMotion = ReducedMotion.Reduce
            },
            async page =>
            {
                await page.GotoAsync("/");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await EnsureLoggedOutAsync(page);

                var link = page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
                await Assertions.Expect(link).ToBeVisibleAsync();

                var animationName = await link.EvaluateAsync<string>("el => getComputedStyle(el).animationName");
                animationName.ShouldBe("none");

                var fontWeight = await link.EvaluateAsync<string>("el => getComputedStyle(el).fontWeight");
                int.TryParse(fontWeight, out var weight).ShouldBeTrue();
                weight.ShouldBeGreaterThanOrEqualTo(700);

                var borderWidth = await link.EvaluateAsync<string>("el => getComputedStyle(el).borderTopWidth");
                float.TryParse(borderWidth.Replace("px", ""), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var bw).ShouldBeTrue();
                bw.ShouldBeGreaterThanOrEqualTo(2f);
            });
    }

    [Test, Retry(2)]
    public async Task LoggedOutHeader_LoginLink_KeyboardFocus_HasVisibleOutline()
    {
        await EnsureLoggedOutAsync(Page);

        var link = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(link).ToBeVisibleAsync();
        await link.FocusAsync();

        var outlineWidth = await link.EvaluateAsync<string>("el => getComputedStyle(el).outlineWidth");
        float.TryParse(outlineWidth.Replace("px", ""), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var ow).ShouldBeTrue();
        ow.ShouldBeGreaterThan(0f);
    }

    [Test, Retry(2)]
    public async Task LoggedOutHeader_NarrowViewport_TitleAndLoginDoNotOverlap()
    {
        await RunWithExtraBrowserContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = ServerFixture.ApplicationBaseUrl,
                IgnoreHTTPSErrors = true,
                ViewportSize = new ViewportSize { Width = 390, Height = 844 }
            },
            async page =>
            {
                await page.GotoAsync("/");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await EnsureLoggedOutAsync(page);

                var title = page.Locator(".header-title h3").First;
                var link = page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
                await Assertions.Expect(title).ToBeVisibleAsync();
                await Assertions.Expect(link).ToBeVisibleAsync();

                var overlap = await page.EvaluateAsync<bool>(
                    @"() => {
                        const t = document.querySelector('.header-title h3');
                        const a = document.querySelector('[data-testid=""LoginLink""]');
                        if (!t || !a) return true;
                        const r1 = t.getBoundingClientRect();
                        const r2 = a.getBoundingClientRect();
                        const xOverlap = r1.left < r2.right && r2.left < r1.right;
                        const yOverlap = r1.top < r2.bottom && r2.top < r1.bottom;
                        return xOverlap && yOverlap;
                    }");
                overlap.ShouldBeFalse();
            });
    }
}
