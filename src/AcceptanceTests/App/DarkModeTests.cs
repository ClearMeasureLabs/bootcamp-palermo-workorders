using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class DarkModeTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task DarkMode_ShouldToggleHtmlDataTheme_WhenSwitchChangedOnSettings()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Settings));
        await Page.WaitForURLAsync("**/settings");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.GetByTestId(nameof(Settings.Elements.DarkModeSwitch)).WaitForAsync();

        var initial = await Page.EvaluateAsync<string>(
            "() => document.documentElement.getAttribute('data-theme')");
        initial.ShouldNotBeNull();

        await Click(nameof(Settings.Elements.DarkModeSwitch));
        await Page.WaitForFunctionAsync(
            "(t) => document.documentElement.getAttribute('data-theme') !== t",
            initial);

        var afterToggle = await Page.EvaluateAsync<string>(
            "() => document.documentElement.getAttribute('data-theme')");
        afterToggle.ShouldNotBe(initial);

        var bsTheme = await Page.EvaluateAsync<string>(
            "() => document.documentElement.getAttribute('data-bs-theme')");
        bsTheme.ShouldBe(afterToggle);

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var onSearch = await Page.EvaluateAsync<string>(
            "() => document.documentElement.getAttribute('data-theme')");
        onSearch.ShouldBe(afterToggle);
    }

    [Test, Retry(2)]
    public async Task DarkMode_ShouldPersistAcrossReload()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Settings));
        await Page.WaitForURLAsync("**/settings");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var darkSwitch = Page.GetByTestId(nameof(Settings.Elements.DarkModeSwitch));
        await darkSwitch.WaitForAsync();

        if (await darkSwitch.IsCheckedAsync())
            await Click(nameof(Settings.Elements.DarkModeSwitch));
        await Page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-theme') === 'light'");

        await Click(nameof(Settings.Elements.DarkModeSwitch));
        await Page.WaitForFunctionAsync(
            "() => document.documentElement.getAttribute('data-theme') === 'dark'");

        var stored = await Page.EvaluateAsync<string>(
            "() => localStorage.getItem('churchbulletin-theme')");
        stored.ShouldBe("dark");

        await Page.ReloadAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var afterReload = await Page.EvaluateAsync<string>(
            "() => document.documentElement.getAttribute('data-theme')");
        afterReload.ShouldBe("dark");
    }

    [Test, Retry(2)]
    public async Task Settings_ShouldRequireAuth()
    {
        await Page.GotoAsync("/settings");
        await Page.WaitForURLAsync("**/login");
        await Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(".*/login.*"));
    }
}
