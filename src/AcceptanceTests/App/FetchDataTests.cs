using ClearMeasure.Bootcamp.UI.Shared.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.App;

[TestFixture]
public class FetchDataTests : AcceptanceTestBase
{
    [Test, Retry(2)]
    public async Task Should_DisplayFiveForecastRows_WhenOpeningFetchDataPage()
    {
        await LoginAsCurrentUser();
        await Page.GotoAsync("/fetchdata");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var table = Page.GetByTestId(nameof(FetchData.Elements.ForecastTableTooltip));
        await table.WaitForAsync();
        await Expect(table).ToBeVisibleAsync();

        var rows = table.Locator("tbody tr.weather-row");
        await Expect(rows).ToHaveCountAsync(5);

        for (var i = 0; i < 5; i++)
        {
            var row = rows.Nth(i);
            await Expect(row.Locator("td.date-cell")).Not.ToBeEmptyAsync();
            await Expect(row.Locator("td.temp-cell").Nth(0)).Not.ToBeEmptyAsync();
            await Expect(row.Locator("td.temp-cell").Nth(1)).Not.ToBeEmptyAsync();
            await Expect(row.Locator("td.summary-cell")).Not.ToBeEmptyAsync();
        }
    }
}
