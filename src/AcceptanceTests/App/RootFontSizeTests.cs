using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class RootFontSizeTests : AcceptanceTestBase
{
    private static async Task WaitForLoginReadyAsync(IPage page)
    {
        await page.GotoAsync("/login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.GetByTestId(nameof(Login.Elements.LoginButton)).WaitForAsync();
    }

    private static Task<string> GetRootFontSizeAsync(IPage page) =>
        page.EvaluateAsync<string>("() => getComputedStyle(document.documentElement).fontSize");

    [Test, Retry(2)]
    public async Task Should_ApplyDesktopRootFontSize_Of15px()
    {
        await WaitForLoginReadyAsync(Page);

        var fontSize = await GetRootFontSizeAsync(Page);

        fontSize.ShouldBe("15px");
    }

    [Test, Retry(2)]
    public async Task Should_ApplyMobileRootFontSize_Of13px()
    {
        await Page.SetViewportSizeAsync(375, 667);
        await WaitForLoginReadyAsync(Page);

        var fontSize = await GetRootFontSizeAsync(Page);

        fontSize.ShouldBe("13px");
    }

    [Test, Retry(2)]
    public async Task Should_KeepLoginPrimaryButton_AtLeast44pxTall_OnDesktop()
    {
        await WaitForLoginReadyAsync(Page);

        var button = Page.GetByTestId(nameof(Login.Elements.LoginButton));
        var box = await button.BoundingBoxAsync();

        box.ShouldNotBeNull();
        box!.Height.ShouldBeGreaterThanOrEqualTo(44);
    }

    [Test, Retry(2)]
    public async Task Should_UseSameRootFontSize_InDarkTheme()
    {
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
