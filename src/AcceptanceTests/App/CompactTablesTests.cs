using System.Text.RegularExpressions;
using ClearMeasure.Bootcamp.UI.Shared;
using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class CompactTablesTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task CompactTables_ShouldApplyClassOnSearchResults_WhenEnabledInSettings()
    {
        await LoginAsCurrentUser();
        await Click(nameof(NavMenu.Elements.Settings));
        await Page.WaitForURLAsync("**/settings");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.GetByTestId(nameof(Settings.Elements.CompactTablesSwitch)).WaitForAsync();

        var compactSwitch = Page.GetByTestId(nameof(Settings.Elements.CompactTablesSwitch));
        if (!await compactSwitch.IsCheckedAsync())
            await Click(nameof(Settings.Elements.CompactTablesSwitch));

        await Page.WaitForFunctionAsync(
            "() => localStorage.getItem('churchbulletin-compact-tables') === 'true'");

        await Click(nameof(NavMenu.Elements.Search));
        await Page.WaitForURLAsync("**/workorder/search");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var table = Page.Locator("table.grid-data");
        await table.WaitForAsync();
        await Expect(table).ToHaveClassAsync(new Regex("\\btable-compact\\b"));
    }
}
