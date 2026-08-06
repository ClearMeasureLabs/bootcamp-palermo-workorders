using ClearMeasure.Bootcamp.UI.Shared.Pages;
using Login = ClearMeasure.Bootcamp.UI.Shared.Pages.Login;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

/// <summary>
/// Playwright checks for the global root typography step-down (desktop 15px / mobile 13px).
/// </summary>
[TestFixture]
public class RootFontSizeTests : AcceptanceTestBase
{
    private static Task<string> GetRootFontSizeAsync(IPage page) =>
        page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).fontSize");

    [Test, Retry(2)]
    public async Task Should_ApplyDesktopRootFontSize_Of15px()
    {
        await Page.SetViewportSizeAsync(1280, 720);
        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var fontSize = await GetRootFontSizeAsync(Page);

        fontSize.ShouldBe("15px");
    }

    [Test, Retry(2)]
    public async Task Should_ApplyMobileRootFontSize_Of13px()
    {
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var fontSize = await GetRootFontSizeAsync(Page);

        fontSize.ShouldBe("13px");
    }

    [Test, Retry(2)]
    public async Task Should_KeepLoginPrimaryButton_AtLeast44pxTall_OnDesktop()
    {
        await Page.SetViewportSizeAsync(1280, 720);
        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var loginButton = Page.GetByTestId(nameof(Login.Elements.LoginButton));
        await loginButton.WaitForAsync();
        var box = await loginButton.BoundingBoxAsync();

        box.ShouldNotBeNull();
        box!.Height.ShouldBeGreaterThanOrEqualTo(44);
    }

    [Test, Retry(2)]
    public async Task Should_KeepLoginPrimaryButton_AtLeast44pxTall_OnMobile()
    {
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync("/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var loginButton = Page.GetByTestId(nameof(Login.Elements.LoginButton));
        await loginButton.WaitForAsync();
        var box = await loginButton.BoundingBoxAsync();

        box.ShouldNotBeNull();
        box!.Height.ShouldBeGreaterThanOrEqualTo(44);
    }

    [Test, Retry(2)]
    public async Task Should_UseSameRootFontSize_InDarkTheme()
    {
        await Page.SetViewportSizeAsync(1280, 720);
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Settings));
        await Page.WaitForURLAsync("**/settings");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var darkSwitch = Page.GetByTestId(nameof(Settings.Elements.DarkModeSwitch));
        await darkSwitch.WaitForAsync();

        if (!await darkSwitch.IsCheckedAsync())
        {
            await Click(nameof(Settings.Elements.DarkModeSwitch));
            await Page.WaitForFunctionAsync(
                "() => document.documentElement.getAttribute('data-theme') === 'dark'");
        }

        var theme = await Page.EvaluateAsync<string>(
            "() => document.documentElement.getAttribute('data-theme')");
        theme.ShouldBe("dark");

        var fontSize = await GetRootFontSizeAsync(Page);
        fontSize.ShouldBe("15px");
    }
}
