using ClearMeasure.Bootcamp.UI.Shared.Components;

namespace ClearMeasure.Bootcamp.AcceptanceTests.Authentication;

[TestFixture]
public class LoginLinkHeaderTests : AcceptanceTestBase
{
    protected override bool NavigateToApplicationRootOnSetup => false;

    private static bool RectanglesOverlap((double Left, double Top, double Right, double Bottom) a,
        (double Left, double Top, double Right, double Bottom) b)
    {
        return !(a.Right <= b.Left || a.Left >= b.Right || a.Bottom <= b.Top || a.Top >= b.Bottom);
    }

    private async Task EnsureLoggedOutOnHomeAsync()
    {
        var page = Page;
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await page.GotoAsync("/");
                await page.WaitForURLAsync("/");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                break;
            }
            catch (PlaywrightException) when (attempt < maxRetries)
            {
                await Task.Delay(2000);
            }
        }

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
    public async Task LoggedOutLoginLink_HasActiveAnimationWhenMotionAllowed()
    {
        await EnsureLoggedOutOnHomeAsync();

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        var animationName = await loginLink.EvaluateAsync<string>("el => getComputedStyle(el).animationName");
        animationName.ShouldNotBe("none");

        var opacity1 = await loginLink.EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).opacity)");
        await Task.Delay(900);
        var opacity2 = await loginLink.EvaluateAsync<double>("el => parseFloat(getComputedStyle(el).opacity)");
        Math.Abs(opacity1 - opacity2).ShouldBeGreaterThan(0.05);
    }

    [Test, Retry(2)]
    public async Task LoggedOutLoginLink_ReducedMotion_DisablesAnimation_KeepsStaticEmphasis()
    {
        var prior = CurrentTestState;
        await prior.BrowserContext.CloseAsync();
        await prior.Browser.CloseAsync();

        var x = RandomPosition.Next(0, 1200);
        var y = RandomPosition.Next(0, 700);
        var browser = await ServerFixture.Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Headless,
            SlowMo = ServerFixture.SlowMo,
            Args = [$"--window-position={x},{y}", "--window-size=800,600"]
        });

        var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = ServerFixture.ApplicationBaseUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 800, Height = 600 },
            ReducedMotion = ReducedMotion.Reduce
        });
        browserContext.SetDefaultTimeout(60_000);

        await browserContext.Tracing.StartAsync(new TracingStartOptions
        {
            Title = $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}",
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });

        var page = await browserContext.NewPageAsync();
        ReplaceTestState(new TestState
        {
            Page = page,
            BrowserContext = browserContext,
            Browser = browser,
            CurrentUser = prior.CurrentUser,
            TestTag = prior.TestTag
        });

        await page.GotoAsync("/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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

        var loginLink = page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();

        var animationName = await loginLink.EvaluateAsync<string>("el => getComputedStyle(el).animationName");
        animationName.ShouldBe("none");

        var fontWeight = await loginLink.EvaluateAsync<string>("el => getComputedStyle(el).fontWeight");
        int.TryParse(fontWeight, out var weight).ShouldBeTrue();
        weight.ShouldBeGreaterThanOrEqualTo(700);

        var borderWidth = await loginLink.EvaluateAsync<string>("el => getComputedStyle(el).borderTopWidth");
        double.TryParse(borderWidth.Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture, out var borderPx).ShouldBeTrue();
        borderPx.ShouldBeGreaterThanOrEqualTo(2);

    }

    [Test, Retry(2)]
    public async Task LoggedOutLoginLink_FocusVisible_HasOutline()
    {
        await EnsureLoggedOutOnHomeAsync();

        var loginLink = Page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(loginLink).ToBeVisibleAsync();
        await loginLink.FocusAsync();

        var outlineWidth = await loginLink.EvaluateAsync<double>(
            "el => parseFloat(getComputedStyle(el).outlineWidth) || 0");
        outlineWidth.ShouldBeGreaterThan(0);
    }

    [Test, Retry(2)]
    public async Task LoggedOutLoginLink_NarrowViewport_TitleAndLinkVisibleWithoutOverlap()
    {
        var prior = CurrentTestState;
        await prior.BrowserContext.CloseAsync();
        await prior.Browser.CloseAsync();

        var x = RandomPosition.Next(0, 1200);
        var y = RandomPosition.Next(0, 700);
        var browser = await ServerFixture.Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Headless,
            SlowMo = ServerFixture.SlowMo,
            Args = [$"--window-position={x},{y}", "--window-size=800,600"]
        });

        var browserContext = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = ServerFixture.ApplicationBaseUrl,
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 390, Height = 844 }
        });
        browserContext.SetDefaultTimeout(60_000);

        await browserContext.Tracing.StartAsync(new TracingStartOptions
        {
            Title = $"{TestContext.CurrentContext.Test.ClassName}.{TestContext.CurrentContext.Test.Name}",
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });

        var page = await browserContext.NewPageAsync();
        ReplaceTestState(new TestState
        {
            Page = page,
            BrowserContext = browserContext,
            Browser = browser,
            CurrentUser = prior.CurrentUser,
            TestTag = prior.TestTag
        });

        await page.GotoAsync("/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

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

        var titleHeading = page.GetByRole(AriaRole.Heading, new() { Name = "Work Order Management" });
        var loginLink = page.GetByTestId(nameof(LoginLink.Elements.LoginLink));
        await Expect(titleHeading).ToBeVisibleAsync();
        await Expect(loginLink).ToBeVisibleAsync();

        var titleBox = await titleHeading.BoundingBoxAsync();
        var linkBox = await loginLink.BoundingBoxAsync();
        titleBox.ShouldNotBeNull();
        linkBox.ShouldNotBeNull();

        var titleRect = (titleBox!.X, titleBox.Y, titleBox.X + titleBox.Width, titleBox.Y + titleBox.Height);
        var linkRect = (linkBox!.X, linkBox.Y, linkBox.X + linkBox.Width, linkBox.Y + linkBox.Height);
        RectanglesOverlap(titleRect, linkRect).ShouldBeFalse();
    }
}
